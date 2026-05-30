using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CargoFlow.Api.Data;
using CargoFlow.Api.Models;

namespace CargoFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TakeCargoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TakeCargoController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 POST - Seed cargos (test data)
        [HttpPost("seed")]
        public async Task<IActionResult> SeedCargos()
        {
            try
            {
                // Delete existing cargos
                var existingCargos = await _context.TakeCargos.ToListAsync();
                if (existingCargos.Any())
                {
                    _context.TakeCargos.RemoveRange(existingCargos);
                    await _context.SaveChangesAsync();
                }

                var testCargos = new List<TakeCargo>
                {
                    new TakeCargo { CargoName = "Cargo 1", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 2", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 3", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 4", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 5", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 6", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Ecargo 1", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Ecargo 2", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Savana", Status = "Available", CreatedAt = DateTime.UtcNow }
                };

                _context.TakeCargos.AddRange(testCargos);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Seeded {testCargos.Count} cargos");
                return Ok(new { message = $"✅ {testCargos.Count} cargos created" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Seed error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔄 POST - Reset database (delete all cargos and reseed)
        [HttpPost("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            try
            {
                // Delete all cargos
                var existingCargos = await _context.TakeCargos.ToListAsync();
                if (existingCargos.Any())
                {
                    _context.TakeCargos.RemoveRange(existingCargos);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"🗑️ Deleted {existingCargos.Count} cargos");
                }

                var testCargos = new List<TakeCargo>
                {
                    new TakeCargo { CargoName = "Cargo 1", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 2", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 3", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 4", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 5", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Cargo 6", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Ecargo 1", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Ecargo 2", Status = "Available", CreatedAt = DateTime.UtcNow },
                    new TakeCargo { CargoName = "Savana", Status = "Available", CreatedAt = DateTime.UtcNow }
                };

                _context.TakeCargos.AddRange(testCargos);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Reset complete: {testCargos.Count} cargos recreated");
                return Ok(new { message = $"✅ Database reset! {testCargos.Count} cargos created (all Available, all unassigned)" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reset error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableCargos()
        {
            try
            {
                var cargos = await _context.TakeCargos
                    .Select(c => new
                    {
                        c.Id,
                        c.CargoName,
                        Status = c.Status,
                        AssignedDriver = c.AssignedDriver ?? "None",
                        c.CreatedAt,
                        c.Gas,
                        c.TakenAt,
                        c.DriverFullName,
                        c.GasReturn,
                        c.LeaveDatetime
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                Console.WriteLine($"📦 Fetched {cargos.Count} cargos");
                return Ok(cargos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching cargos: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 POST - Create a new cargo
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TakeCargo data)
        {
            try
            {
                data.CreatedAt = DateTime.UtcNow;
                data.Status = "Available";
                
                _context.TakeCargos.Add(data);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cargo created: {data.CargoName}");
                return Ok(new { message = "Cargo saved successfully", id = data.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating cargo: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 PUT - Assign a cargo to a driver
        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignCargo(int id, [FromBody] AssignCargoDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.DriverName))
                {
                    Console.WriteLine($"❌ Invalid request: missing DriverName");
                    return BadRequest(new { error = "Missing DriverName parameter" });
                }

                var cargo = await _context.TakeCargos.FindAsync(id);
                if (cargo == null)
                {
                    Console.WriteLine($"❌ Cargo not found: {id}");
                    return NotFound(new { error = "Cargo not found" });
                }

                if (!string.IsNullOrEmpty(cargo.AssignedDriver))
                {
                    Console.WriteLine($"❌ Cargo {id} already assigned to {cargo.AssignedDriver}");
                    return BadRequest(new { error = "This cargo is already assigned to another driver!" });
                }

                cargo.AssignedDriver = request.DriverName;
                cargo.AssignedAt = DateTime.UtcNow;
                cargo.Status = "Pending";

                _context.TakeCargos.Update(cargo);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cargo {id} assigned to {request.DriverName}");
                return Ok(new { message = "Cargo assigned successfully", cargoId = cargo.Id, driver = request.DriverName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error assigning cargo: {ex.Message}\nStack: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Error: {ex.Message}" });
            }
        }

        // POST - Upload a single photo and return its URL
        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No file provided" });

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(file.FileName) is { Length: > 0 } e ? e : ".jpg";
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var request = HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{fileName}";

                Console.WriteLine($"✅ Photo uploaded: {fileUrl}");
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET - Driver history (all cargos taken by a driver)
        [HttpGet("history/{driverName}")]
        public async Task<IActionResult> GetDriverHistory(string driverName)
        {
            try
            {
                var cargos = await _context.TakeCargos
                    .Where(c => c.DriverFullName != null && c.DriverFullName.ToLower() == driverName.ToLower())
                    .Select(c => new
                    {
                        c.Id,
                        c.CargoName,
                        c.Gas,
                        c.GasReturn,
                        c.HasGarbage,
                        c.Status,
                        c.DriverFullName,
                        c.DriverId,
                        c.DashboardImage,
                        c.ExteriorImages,
                        c.GarbageImage,
                        c.TakenAt,
                        c.LeaveDatetime,
                        c.AssignedAt,
                        c.CompletedAt,
                        c.CreatedAt
                    })
                    .OrderByDescending(c => c.TakenAt)
                    .ToListAsync();

                Console.WriteLine($"📋 History for {driverName}: {cargos.Count} cargos");
                return Ok(cargos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching history: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // 🔥 POST - Record pickup with actual file uploads
        [HttpPost("record-pickup")]
        public async Task<IActionResult> RecordPickup([FromForm] int cargoId, [FromForm] double gas, [FromForm] bool hasGarbage, [FromForm] string? driverFullName, [FromForm] int? driverId, [FromForm] string? takenAt, [FromForm] string? dashboardPhoto, [FromForm] string? exteriorPhotos, [FromForm] string? garbagePhoto)
        {
            try
            {
                var cargo = await _context.TakeCargos.FindAsync(cargoId);
                if (cargo == null)
                    return NotFound("Cargo not found");

                // Update cargo with pickup details
                cargo.Gas = gas;
                cargo.HasGarbage = hasGarbage;
                cargo.DriverFullName = driverFullName;
                cargo.DriverId = driverId;
                cargo.DashboardImage = dashboardPhoto;
                cargo.ExteriorImages = exteriorPhotos;
                cargo.GarbageImage = garbagePhoto;
                cargo.TakenAt = !string.IsNullOrEmpty(takenAt)
                    ? DateTimeOffset.Parse(takenAt).UtcDateTime
                    : DateTime.UtcNow;

                _context.TakeCargos.Update(cargo);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Pickup recorded for cargo: {cargo.CargoName}");

                // 📲 Notify admin via Expo push
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var adminTokens = await _context.PushTokens
                            .Where(t => t.IsAdmin)
                            .Select(t => t.Token)
                            .ToListAsync();

                        if (adminTokens.Any())
                        {
                            using var http = new HttpClient();
                            foreach (var token in adminTokens)
                            {
                                var payload = new
                                {
                                    to = token,
                                    title = $"u{driverId} TAKE {cargo.CargoName}",
                                    body = $"Driver u{driverId} picked up {cargo.CargoName}",
                                    sound = "default",
                                    data = new { cargoId = cargo.Id }
                                };
                                var content = new System.Net.Http.StringContent(
                                    System.Text.Json.JsonSerializer.Serialize(payload),
                                    System.Text.Encoding.UTF8,
                                    "application/json");
                                await http.PostAsync("https://exp.host/--/api/v2/push/send", content);
                            }
                            Console.WriteLine($"📲 Admin push sent to {adminTokens.Count} device(s)");
                        }
                    }
                    catch (Exception pushEx)
                    {
                        Console.WriteLine($"⚠️ Push notification error: {pushEx.Message}");
                    }
                });

                return Ok(new { message = "Pickup recorded successfully", id = cargo.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error recording pickup: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 POST - Create a cargo with photos (from mobile)
        [HttpPost("create-with-photos")]
        public async Task<IActionResult> CreateWithPhotos([FromForm] string cargoName, [FromForm] double gas, [FromForm] bool hasGarbage, [FromForm] string? driverFullName, [FromForm] int? driverId, [FromForm] string? dashboardPhoto, [FromForm] string? exteriorPhotos, [FromForm] string? garbagePhoto)
        {
            try
            {
                var takeCargo = new TakeCargo
                {
                    CargoName = cargoName,
                    Gas = gas,
                    HasGarbage = hasGarbage,
                    DriverFullName = driverFullName,
                    DriverId = driverId,
                    DashboardImage = dashboardPhoto, // Store as string URI
                    ExteriorImages = exteriorPhotos, // Store as JSON array string
                    GarbageImage = garbagePhoto, // Store as string URI
                    TakenAt = DateTime.UtcNow,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.TakeCargos.Add(takeCargo);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cargo created: {cargoName}");
                return Ok(new { message = "Cargo recorded successfully", id = takeCargo.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating cargo: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 PUT - Release a cargo (mark as delivered)
        [HttpPut("{id}/release")]
        public async Task<IActionResult> ReleaseCargo(int id)
        {
            try
            {
                var cargo = await _context.TakeCargos.FindAsync(id);
                if (cargo == null)
                    return NotFound("Cargo not found");

                cargo.AssignedDriver = null;
                cargo.Status = "Available";  // Back to Available for reuse
                cargo.CompletedAt = DateTime.UtcNow;

                _context.TakeCargos.Update(cargo);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cargo released: {cargo.CargoName}");
                return Ok(new { message = "Cargo released successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error releasing cargo: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 PUT - Complete cargo delivery with delivery form
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteCargo(int id, [FromForm] double gasReturn, [FromForm] string? leaveDatetime, [FromForm] bool hasGarbage, [FromForm] string? dashboardPhoto, [FromForm] string? exteriorPhotos, [FromForm] string? garbagePhoto)
        {
            try
            {
                var cargo = await _context.TakeCargos.FindAsync(id);
                if (cargo == null)
                    return NotFound("Cargo not found");

                // Update cargo with delivery information
                cargo.GasReturn = gasReturn;
                cargo.LeaveDatetime = string.IsNullOrEmpty(leaveDatetime) 
                    ? DateTime.UtcNow 
                    : DateTimeOffset.Parse(leaveDatetime).UtcDateTime;
                cargo.HasGarbage = hasGarbage;
                // Save delivery photos separately (do not overwrite pickup photos)
                cargo.DashboardImageLeave = dashboardPhoto;
                cargo.ExteriorImagesLeave = exteriorPhotos;
                cargo.GarbageImageLeave = garbagePhoto;
                cargo.AssignedDriver = null;
                cargo.Status = "Available";
                cargo.CompletedAt = DateTime.UtcNow;

                _context.TakeCargos.Update(cargo);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cargo delivery completed: {cargo.CargoName}");
                return Ok(new { message = "Cargo delivery completed successfully", id = cargo.Id, gasReturn, leaveDatetime = cargo.LeaveDatetime });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error completing cargo: {ex.Message}");
                return StatusCode(500, new { error = $"Error: {ex.Message}" });
            }
        }
    }
}