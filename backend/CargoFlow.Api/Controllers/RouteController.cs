using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using CargoFlow.Api.Models;

namespace CargoFlow.Api.Controllers;

[ApiController]
[Route("api/route")]
public class RouteController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly Regex PostalCodeRegex = new(@"\b[A-Z]\d[A-Z]\s?\d[A-Z]\d\b", RegexOptions.Compiled);
    private static readonly Regex UnitRegex = new(@",?\s*(?:app(?:artement)?|apt|apartment|suite|unit|bureau|#)\s*[\w.-]+\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Persistent cache across requests — survives as long as backend is running
    private static readonly ConcurrentDictionary<string, (double lat, double lng)?> GeoCache =
        new(StringComparer.OrdinalIgnoreCase);
    // Nominatim rate-limit: 1 req/sec strict
    private static readonly SemaphoreSlim NominatimSemaphore = new(1, 1);

    [HttpPost("parse-pdf")]
    public async Task<IActionResult> ParsePdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        if (!file.ContentType.Contains("pdf") && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("File must be a PDF");

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var stops = ParseRouteFromPdf(stream.ToArray());

            // Geocode all unique addresses with timeout
            await GeocodeStopsAsync(stops);

            // Calculate route distance
            var totalDistance = CalculateRouteDistance(stops);

            return Ok(new 
            { 
                stops = stops.OrderBy(s => s.Seq).ToList(),
                totalStops = stops.Count,
                totalDistance = Math.Round(totalDistance, 2)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("test-route")]
    public IActionResult TestRoute()
    {
        // Test data with fake coordinates
        var stops = new List<StopDto>
        {
            new() { Seq = 1, TrackingId = "TEST001", Address = "123 Main St, Toronto, ON M5H 2N2", RouteCode = "D210021769671", Dimensions = "", Lat = 43.6632, Lng = -79.3957 },
            new() { Seq = 2, TrackingId = "TEST002", Address = "456 King St W, Toronto, ON M5V 1L1", RouteCode = "D210021769671", Dimensions = "", Lat = 43.6426, Lng = -79.3871 },
            new() { Seq = 3, TrackingId = "TEST003", Address = "789 Bay St, Toronto, ON M5J 2N8", RouteCode = "D210021769671", Dimensions = "", Lat = 43.6629, Lng = -79.3914 },
        };
        
        var totalDistance = CalculateRouteDistance(stops);
        return Ok(new 
        { 
            stops = stops.OrderBy(s => s.Seq).ToList(),
            totalStops = stops.Count,
            totalDistance = Math.Round(totalDistance, 2)
        });
    }

    private async Task GeocodeStopsAsync(List<StopDto> stops)
    {
        var http = httpClientFactory.CreateClient("geocoder");

        // Strip unit from address before geocoding (App 101, Suite 5, etc.)
        // → one request per building, not per unit
        string ToBaseAddress(string address)
        {
            var noPostal = PostalCodeRegex.Replace(address, "").Trim().TrimEnd(',').Trim();
            return UnitRegex.Replace(noPostal, "").Trim().TrimEnd(',').Trim();
        }

        // Unique building base addresses not already cached
        var uniqueBases = stops
            .Select(s => ToBaseAddress(s.Address))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(a => !GeoCache.ContainsKey(a))
            .ToList();

        // Nominatim: max 1 req/sec — process sequentially with 1.5-second delay (conservative)
        // Max 90 seconds total to avoid timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        
        try
        {
            foreach (var baseAddr in uniqueBases)
            {
                if (cts.Token.IsCancellationRequested) break; // Stop if timeout approaching
                
                await NominatimSemaphore.WaitAsync(cts.Token);
                try
                {
                    var coords = await GeocodeAsync(http, baseAddr);
                    GeoCache.TryAdd(baseAddr, coords);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Nominatim rate limited — wait longer before retry
                    await Task.Delay(3000, cts.Token);
                }
                finally
                {
                    NominatimSemaphore.Release();
                }
                
                await Task.Delay(1500, cts.Token); // 1.5 sec between requests (conservative)
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached — return with partial geocoding results
        }

        // Apply coordinates to all stops (match by base address)
        foreach (var stop in stops)
        {
            var baseAddr = ToBaseAddress(stop.Address);
            if (GeoCache.TryGetValue(baseAddr, out var coords) && coords.HasValue)
            {
                stop.Lat = coords.Value.lat;
                stop.Lng = coords.Value.lng;
            }
        }
    }

    private async Task<(double lat, double lng)?> GeocodeAsync(HttpClient http, string baseAddress)
    {
        try
        {
            var query = Uri.EscapeDataString(baseAddress + ", Gatineau, QC, Canada");
            var url = $"/search?q={query}&format=json&limit=1&countrycodes=ca";

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.GetArrayLength() == 0) return null;

            var first = doc.RootElement[0];
            var lat = double.Parse(first.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            var lon = double.Parse(first.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            return (lat, lon);
        }
        catch
        {
            return null;
        }
    }

    // PDF column layout (left → right):
    // Code (D+12digits) | Tracking ID (INTLCMI...) | Seq (1-999) | Address (...CA) | Dimensions | Signature
    private List<StopDto> ParseRouteFromPdf(byte[] pdfBytes)
    {
        var stops = new List<StopDto>();

        using var doc = PdfDocument.Open(pdfBytes);

        foreach (var page in doc.GetPages())
        {
            var pageWords = page.GetWords()
                .Select(w => (X: w.BoundingBox.Left, Y: w.BoundingBox.Bottom, Text: w.Text))
                .ToList();

            if (!pageWords.Any()) continue;

            // Find "Seq" column header X to pinpoint the seq column
            var seqHeader = pageWords.FirstOrDefault(w =>
                string.Equals(w.Text, "Seq", StringComparison.OrdinalIgnoreCase));
            double seqColX = seqHeader.Text != null ? seqHeader.X : -1;

            // Group into rows by Y position (5-point tolerance)
            var rows = pageWords
                .GroupBy(w => Math.Round(w.Y / 5) * 5)
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var rowGroup in rows)
            {
                var row = rowGroup.OrderBy(w => w.X).ToList();

                // Must have a tracking ID to be a data row
                var trackingWord = row.FirstOrDefault(w => IsTrackingId(w.Text));
                if (trackingWord.Text == null) continue;

                string trackingId = trackingWord.Text;

                // Route code: D followed by exactly 12 digits
                string code = row.FirstOrDefault(w => IsRouteCode(w.Text)).Text ?? "";

                // Find seq using header column X (most reliable)
                int seq = -1;
                double seqWordX = -1;

                if (seqColX > 0)
                {
                    // Find integer closest to the Seq column header X (within ±25 units)
                    foreach (var w in row.Where(wr => Math.Abs(wr.X - seqColX) < 25))
                    {
                        if (int.TryParse(w.Text, out int n) && n >= 1 && n <= 999)
                        {
                            seq = n;
                            seqWordX = w.X;
                            break;
                        }
                    }
                }

                // Fallback: first integer after tracking ID X position
                if (seq < 1)
                {
                    bool afterTracking = false;
                    foreach (var w in row)
                    {
                        if (!afterTracking)
                        {
                            if (w.X >= trackingWord.X) afterTracking = true;
                            continue;
                        }
                        if (int.TryParse(w.Text, out int n) && n >= 1 && n <= 999)
                        {
                            seq = n;
                            seqWordX = w.X;
                            break;
                        }
                    }
                }

                if (seq < 1) continue;

                // Address: all words after seq column X, up to and including "CA"
                // Excludes tracking ID, route code, and the seq number itself
                var addressParts = new List<string>();
                bool addressDone = false;

                foreach (var w in row.Where(wr => wr.X > seqWordX + 3).OrderBy(wr => wr.X))
                {
                    if (addressDone) break;
                    if (IsTrackingId(w.Text) || IsRouteCode(w.Text)) continue;
                    // Skip if it's the seq number itself (same X position)
                    if (w.Text == seq.ToString() && Math.Abs(w.X - seqWordX) < 3) continue;

                    addressParts.Add(w.Text);
                    if (w.Text == "CA") addressDone = true;
                }

                string address = string.Join(" ", addressParts).Trim();

                // Trim anything after "CA" (dimensions may leak in edge cases)
                int caIdx = address.LastIndexOf(" CA");
                if (caIdx >= 0) address = address[..(caIdx + 3)];

                if (address.Length > 5 && !stops.Any(s => s.Seq == seq))
                {
                    stops.Add(new StopDto
                    {
                        Seq = seq,
                        TrackingId = trackingId,
                        Address = address,
                        RouteCode = code
                    });
                }
            }
        }

        return stops.OrderBy(s => s.Seq).ToList();
    }

    // Intelcom / Amazon carrier tracking ID prefixes
    private static bool IsTrackingId(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 6) return false;

        string up = text.ToUpperInvariant();
        if (up.StartsWith("INTLCM")) return true;   // INTLCMI, INTLCMR
        if (up.StartsWith("STRTD")) return true;
        if (up.StartsWith("BAINT")) return true;
        if (up.StartsWith("ALTSH")) return true;
        if (up.StartsWith("STRCA")) return true;
        if (up.StartsWith("LMS") && text.Length > 8) return true;
        if (up.StartsWith("SNH") && text.Length > 8) return true;
        if (up.StartsWith("RET") && text.Length > 10) return true;
        if (up.StartsWith("SE") && text.Length > 8) return true;
        if (up.StartsWith("PG") && text.Length > 8) return true;
        // Traditional carriers
        if (up.StartsWith("TBA") && text.Length > 6) return true;
        if (up.StartsWith("1Z") && text.Length > 10) return true;
        if (up.StartsWith("JD") && text.Length > 10) return true;
        if (text.All(char.IsDigit) && text.Length >= 12) return true;

        return false;
    }

    // Route code: D followed by exactly 12 digits (e.g. D210021769671)
    private static bool IsRouteCode(string text) =>
        text.Length == 13 && text[0] == 'D' && text[1..].All(char.IsDigit);

    // Calculate total route distance (km)
    private double CalculateRouteDistance(List<StopDto> stops)
    {
        if (stops.Count == 0) return 0;

        double totalDistance = 0;
        
        // Calculate distance between consecutive stops using Haversine formula
        for (int i = 0; i < stops.Count - 1; i++)
        {
            if (stops[i].Lat.HasValue && stops[i].Lng.HasValue &&
                stops[i + 1].Lat.HasValue && stops[i + 1].Lng.HasValue)
            {
                totalDistance += HaversineDistance(
                    stops[i].Lat.Value, stops[i].Lng.Value,
                    stops[i + 1].Lat.Value, stops[i + 1].Lng.Value
                );
            }
        }

        Console.WriteLine($"\n📊 ROUTE DISTANCE:");
        Console.WriteLine($"  📦 Total stops: {stops.Count}");
        Console.WriteLine($"  📍 Total distance: {Math.Round(totalDistance, 2)} km\n");

        return totalDistance;
    }

    // Haversine formula to calculate distance between two coordinates
    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }


}

