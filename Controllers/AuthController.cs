using Microsoft.AspNetCore.Mvc;
using Concert_Backend.Data;
using Concert_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Concert_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context; // FIXED name

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginData)
        {
            // Find user in the Users table
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginData.Email);

            if (user == null || user.PasswordHash != loginData.Password) 
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            // Return data exactly as React expects it
            return Ok(new { 
                name = user.Name, 
                email = user.Email, 
                role = user.Role 
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto regData)
        {
            if (await _context.Users.AnyAsync(u => u.Email == regData.Email))
            {
                return BadRequest(new { message = "Email already registered" });
            }

            var newUser = new User 
            {
                Name = regData.FullName,
                Email = regData.Email,
                PasswordHash = regData.Password, // Plain text for now, hash later
                Role = "Customer",
                PhoneNumber = regData.Phone
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful" });
        }
    }

    public class LoginDto { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
    public class RegisterDto { 
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Password { get; set; } = "";
    }
}