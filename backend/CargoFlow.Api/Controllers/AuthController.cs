using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CargoFlow.Api.Data;
using CargoFlow.Api.Models;

namespace CargoFlow.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 TEST ROUTE - To verify the server is running
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "✅ Backend is running!", timestamp = DateTime.Now });
        }

        // 🔥 REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                Console.WriteLine($"📝 Register attempt: {user.Email}");
                Console.WriteLine($"   Full Name: {user.FullName}");
                Console.WriteLine($"   Driver License: {user.DriverLicense}");
                Console.WriteLine($"   Delivery ID: {user.DeliveryId}");
                
                var exist = await _context.Users.AnyAsync(x => x.Email == user.Email);

                if (exist)
                    return BadRequest("Email already exists");

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ User created: {user.Email}");
                return Ok("User created");
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"❌ Database error: {dbEx.InnerException?.Message}");
                return StatusCode(500, $"Database error: {dbEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Register error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 LOGIN + DEBUG
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                Console.WriteLine($"🔑 Login attempt: {dto.Email}");

                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == dto.Email);

                if (user == null)
                    return BadRequest("❌ Email not found");

                if (user.Password != dto.Password)
                    return BadRequest("❌ Password incorrect");

                Console.WriteLine($"✅ Login successful: {user.Email}");
                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.DriverLicense,
                    user.DeliveryId,
                    user.IsAdmin
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 GET DRIVER PROFILE
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound("Driver not found");

                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.DriverLicense,
                    user.DeliveryId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // 🔥 PUT - UPDATE USER PROFILE
        [HttpPut("profile/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] User updatedUser)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound("User not found");

                // Update fields
                user.FullName = updatedUser.FullName ?? user.FullName;
                user.Email = updatedUser.Email ?? user.Email;
                user.DriverLicense = updatedUser.DriverLicense ?? user.DriverLicense;
                user.DeliveryId = updatedUser.DeliveryId ?? user.DeliveryId;
                if (!string.IsNullOrEmpty(updatedUser.Password))
                    user.Password = updatedUser.Password;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ User updated: {user.Email}");
                return Ok(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.DriverLicense,
                    user.DeliveryId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Update error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}