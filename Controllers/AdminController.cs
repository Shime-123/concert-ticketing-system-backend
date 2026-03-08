using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;
using Concert_Backend.Models;
using System.Text;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. GET DASHBOARD STATS ---
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var totalRevenue = await _context.Tickets.SumAsync(t => (double?)t.Price) ?? 0;
                var totalTickets = await _context.Purchases.SumAsync(p => (int?)p.Quantity) ?? 0;

                var recentPurchases = await _context.Purchases
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new {
                        p.PaymentId,
                        p.UserEmail,
                        p.TicketType,
                        p.Quantity,
                        p.CreatedAt,
                        ConcertTitle = _context.Tickets
                            .Where(t => t.PaymentId == p.PaymentId)
                            .Select(t => t.Concert.ConcertTitle)
                            .FirstOrDefault() ?? "Concert"
                    })
                    .ToListAsync();

                return Ok(new { totalRevenue, totalTickets, recentPurchases });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Stats error", error = ex.Message });
            }
        }

        // --- 2. USER MANAGEMENT (Crucial for Frontend) ---
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new { u.Name, u.Email, u.Role, u.IsSuspended })
                    .ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching users", error = ex.Message });
            }
        }

        [HttpPut("toggle-suspension/{email}")]
        public async Task<IActionResult> ToggleSuspension(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            user.IsSuspended = !user.IsSuspended;
            await _context.SaveChangesAsync();
            return Ok(new { message = user.IsSuspended ? "User suspended" : "User unsuspended", isSuspended = user.IsSuspended });
        }

        [HttpPut("update-role")]
        public async Task<IActionResult> UpdateRole([FromBody] RoleUpdateDto data)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
            if (user == null) return NotFound("User not found");

            user.Role = data.Role;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Role updated" });
        }

        [HttpDelete("delete-user/{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User deleted" });
        }

        // --- 3. CONCERT MANAGEMENT ---
        [HttpPost("add-concert")]
        public async Task<IActionResult> AddConcert([FromBody] Concert concert)
        {
            _context.Concerts.Add(concert);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Concert added!", concert });
        }

        [HttpPut("update-concert/{id}")]
        public async Task<IActionResult> UpdateConcert(int id, [FromBody] Concert updatedConcert)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();

            concert.ConcertTitle = updatedConcert.ConcertTitle;
            concert.Venue = updatedConcert.Venue;
            concert.Date = updatedConcert.Date;
            concert.ImageUrl = updatedConcert.ImageUrl;
            concert.RegularPrice = updatedConcert.RegularPrice;
            concert.VipPrice = updatedConcert.VipPrice;
            concert.RegularStripeId = updatedConcert.RegularStripeId;
            concert.VipStripeId = updatedConcert.VipStripeId;
            concert.IsSoldOut = updatedConcert.IsSoldOut;

            await _context.SaveChangesAsync();
            return Ok(concert);
        }

        [HttpDelete("delete-concert/{id}")]
        public async Task<IActionResult> DeleteConcert(int id)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();
            _context.Concerts.Remove(concert);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- 4. EXPORT ---
        [HttpGet("export")]
        public async Task<IActionResult> ExportPurchases()
        {
            var purchases = await _context.Purchases.ToListAsync();
            var csv = new StringBuilder().AppendLine("PaymentId,Email,Type,Qty,Date");
            foreach (var p in purchases) csv.AppendLine($"{p.PaymentId},\"{p.UserEmail}\",{p.TicketType},{p.Quantity},{p.CreatedAt}");
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "Report.csv");
        }
    }

    // Helper DTO for Role Updates
    public class RoleUpdateDto
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }
}