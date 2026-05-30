using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CargoFlow.Api.Data;
using CargoFlow.Api.Models;

namespace CargoFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // SETUP: one-time make a user admin by email
        // ─────────────────────────────────────────────
        [HttpPost("make-admin")]
        public async Task<IActionResult> MakeAdmin([FromBody] MakeAdminDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null) return NotFound("User not found");
            user.IsAdmin = true;
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Admin flag set for: {user.Email}");
            return Ok(new { message = "Admin flag set", userId = user.Id });
        }

        // ─────────────────────────────────────────────
        // PUSH TOKENS
        // ─────────────────────────────────────────────
        [HttpPost("push-token")]
        public async Task<IActionResult> SavePushToken([FromBody] PushTokenDto dto)
        {
            var existing = await _context.PushTokens.FirstOrDefaultAsync(t => t.Token == dto.Token);
            if (existing != null)
            {
                existing.UserId = dto.UserId;
                existing.IsAdmin = dto.IsAdmin;
            }
            else
            {
                _context.PushTokens.Add(new PushToken
                {
                    Token = dto.Token,
                    UserId = dto.UserId,
                    IsAdmin = dto.IsAdmin,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Token saved" });
        }

        // ─────────────────────────────────────────────
        // CARGO PENDING (assigned to a driver)
        // ─────────────────────────────────────────────
        [HttpGet("cargo/pending")]
        public async Task<IActionResult> GetPendingCargos()
        {
            var cargos = await _context.TakeCargos
                .Where(c => c.Status == "Pending" && c.AssignedDriver != null)
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
                    c.AssignedDriver,
                    c.DashboardImage,
                    c.ExteriorImages,
                    c.GarbageImage,
                    DashboardImageLeave = (string?)null,
                    ExteriorImagesLeave = (string?)null,
                    c.TakenAt,
                    c.LeaveDatetime,
                    c.AssignedAt,
                    c.CompletedAt,
                    c.CreatedAt
                })
                .OrderByDescending(c => c.TakenAt)
                .ToListAsync();

            Console.WriteLine($"📋 Admin: {cargos.Count} pending cargos");
            return Ok(cargos);
        }

        // ─────────────────────────────────────────────
        // CARGO AVAILABLE (with last driver info)
        // ─────────────────────────────────────────────
        [HttpGet("cargo/available")]
        public async Task<IActionResult> GetAvailableCargos()
        {
            var cargos = await _context.TakeCargos
                .Where(c => c.Status == "Available")
                .Select(c => new
                {
                    c.Id,
                    c.CargoName,
                    c.Gas,
                    c.GasReturn,
                    c.Status,
                    LastDriver = c.DriverFullName,
                    LastDriverId = c.DriverId,
                    c.TakenAt,
                    c.LeaveDatetime,
                    c.CompletedAt,
                    c.CreatedAt
                })
                .OrderByDescending(c => c.CompletedAt ?? c.CreatedAt)
                .ToListAsync();

            Console.WriteLine($"📦 Admin: {cargos.Count} available cargos");
            return Ok(cargos);
        }

        // ─────────────────────────────────────────────
        // GAS COMPLAINTS  (gas pickup > gas return + threshold)
        // Fine = (gasPickup - gasReturn) * 30  [simplified: $30 per unit diff]
        // ─────────────────────────────────────────────
        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaints()
        {
            // A complaint exists when gas return < gas pickup (driver used more gas)
            var complaints = await _context.TakeCargos
                .Where(c =>
                    c.GasReturn != null &&
                    c.Gas > 0 &&
                    c.GasReturn < c.Gas &&
                    c.DriverFullName != null &&
                    c.LeaveDatetime != null)
                .Select(c => new
                {
                    c.Id,
                    c.CargoName,
                    c.DriverFullName,
                    c.DriverId,
                    GasPickup = c.Gas,
                    GasReturn = c.GasReturn,
                    FineAmount = 30.0,
                    c.TakenAt,
                    LeaveAt = c.LeaveDatetime,
                    DashboardImagePickup = c.DashboardImage,
                    DashboardImageLeave = c.DashboardImageLeave,
                })
                .OrderByDescending(c => c.LeaveAt)
                .ToListAsync();

            Console.WriteLine($"⚠️ Admin: {complaints.Count} gas complaints");
            return Ok(complaints);
        }

        // ─────────────────────────────────────────────
        // DISMISS complaint (soft-delete: archives then hides from list)
        // ─────────────────────────────────────────────
        [HttpDelete("complaints/{cargoId}")]
        public async Task<IActionResult> DismissComplaint(int cargoId)
        {
            var cargo = await _context.TakeCargos.FindAsync(cargoId);
            if (cargo == null) return NotFound("Cargo not found");

            // Archive before dismissing (never deletes from DB)
            var archive = new GasComplaintArchive
            {
                CargoId = cargo.Id,
                CargoName = cargo.CargoName,
                DriverFullName = cargo.DriverFullName ?? "",
                DriverId = cargo.DriverId,
                GasPickup = cargo.Gas,
                GasReturn = cargo.GasReturn ?? 0,
                FineAmount = 30.0,
                TakenAt = cargo.TakenAt,
                LeaveAt = cargo.LeaveDatetime,
                DashboardImagePickup = cargo.DashboardImage,
                DashboardImageLeave = cargo.DashboardImageLeave,
                DismissedAt = DateTime.UtcNow
            };
            _context.GasComplaintArchives.Add(archive);

            // Mark as dismissed so it won't appear again: set GasReturn = Gas
            cargo.GasReturn = cargo.Gas;
            await _context.SaveChangesAsync();

            Console.WriteLine($"🗑️ Complaint dismissed for cargo {cargoId}, archived");
            return Ok(new { message = "Complaint dismissed and archived" });
        }

        // ─────────────────────────────────────────────
        // FULL HISTORY (all archived/completed cargos for admin)
        // ─────────────────────────────────────────────
        [HttpGet("cargo/history")]
        public async Task<IActionResult> GetAllHistory()
        {
            var all = await _context.TakeCargos
                .Where(c => c.DriverFullName != null && c.TakenAt != null)
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
                    c.DashboardImageLeave,
                    c.ExteriorImagesLeave,
                    c.GarbageImageLeave,
                    c.TakenAt,
                    c.LeaveDatetime,
                    c.AssignedAt,
                    c.CompletedAt,
                    c.CreatedAt
                })
                .OrderByDescending(c => c.TakenAt)
                .ToListAsync();

            return Ok(all);
        }

        // ─────────────────────────────────────────────
        // SEND PUSH NOTIFICATION to all admin devices
        // ─────────────────────────────────────────────
        [HttpPost("notify-admin")]
        public async Task<IActionResult> NotifyAdmin([FromBody] NotifyDto dto)
        {
            var adminTokens = await _context.PushTokens
                .Where(t => t.IsAdmin)
                .Select(t => t.Token)
                .ToListAsync();

            if (!adminTokens.Any())
            {
                Console.WriteLine("⚠️ No admin push tokens found");
                return Ok(new { message = "No admin tokens", sent = 0 });
            }

            int sent = 0;
            using var http = new HttpClient();
            foreach (var token in adminTokens)
            {
                var payload = new
                {
                    to = token,
                    title = dto.Title,
                    body = dto.Body,
                    data = dto.Data,
                    sound = "default"
                };
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                try
                {
                    await http.PostAsync("https://exp.host/--/api/v2/push/send", content);
                    sent++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Push error: {ex.Message}");
                }
            }
            Console.WriteLine($"📲 Admin notified: {sent} token(s)");
            return Ok(new { message = "Notifications sent", sent });
        }
    }

    public class MakeAdminDto { public string Email { get; set; } = ""; }
    public class PushTokenDto
    {
        public string Token { get; set; } = "";
        public int? UserId { get; set; }
        public bool IsAdmin { get; set; }
    }
    public class NotifyDto
    {
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public object? Data { get; set; }
    }
}
