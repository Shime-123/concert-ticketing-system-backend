using Microsoft.AspNetCore.Mvc;
using Concert_Backend.Data;
using Concert_Backend.Models;
using Concert_Backend.Services; 
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; 

namespace Concert_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService; 

        public AuthController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Use the full path to avoid namespace confusion
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new { 
                name = user.Name, 
                email = user.Email, 
                role = user.Role 
            });
        }

[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null) return NotFound(new { message = "We couldn't find an account with that email." });

    string code = new Random().Next(100000, 999999).ToString();
    
    user.ResetCode = code;
    // FIX 1: Use UtcNow to prevent time-zone expiration bugs
    user.ResetCodeExpiry = DateTime.UtcNow.AddMinutes(15); 
    await _context.SaveChangesAsync();

    // FIX 2: Print to console so you can see the code in Render Logs even if email fails
    Console.WriteLine($"🗝️ DEBUG: Reset code for {user.Email} is {code}");

    // We still call this, but if it hangs, the user has already received the "Ok" response below
    // Note: If you want to prevent the 10s delay, you can remove the 'await' 
    // but keeping it is safer for debugging until Brevo is 100% active.
    await _emailService.SendEmailAsync(
        user.Email, 
        "Your Reset Code - Ethio Concert", 
        $"Your code is: {code}"
    );

    return Ok(new { message = "If the email exists, a code has been sent." });
}

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    
    // FIX 3: Use UtcNow here as well to match the expiry check
    if (user == null || user.ResetCode != request.Code || user.ResetCodeExpiry < DateTime.UtcNow)
    {
        return BadRequest(new { message = "Invalid or expired verification code." });
    }

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    user.ResetCode = null;
    user.ResetCodeExpiry = null;

    await _context.SaveChangesAsync();
    return Ok(new { message = "Password updated!" });
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(regData.Password), 
                Role = "Customer",
                PhoneNumber = regData.Phone
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful" });
        }
    }

    // --- DTOs ---
    public class LoginDto 
    { 
        public string Email { get; set; } = ""; 
        public string Password { get; set; } = ""; 
    }

    public class RegisterDto 
    { 
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ForgotPasswordRequest { public string Email { get; set; } = ""; }

    public class ResetPasswordRequest 
    { 
        public string Email { get; set; } = "";
        public string Code { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}