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

        // --- 0. GET DASHBOARD STATS (Optimized) ---
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] int page = 1)
        {
            try
            {
                const int pageSize = 5;

                // 1. Calculate Totals
                // Use double? to handle nulls if no tickets exist yet
                var totalRevenue = await _context.Tickets.SumAsync(t => (double?)t.Price) ?? 0;
                var totalTickets = await _context.Purchases.SumAsync(p => (int?)p.Quantity) ?? 0;
                var totalPurchasesCount = await _context.Purchases.CountAsync();

                // 2. Fetch Paginated Recent Purchases with Join logic
                var recentPurchases = await _context.Purchases
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new {
                        p.PaymentId,
                        p.UserEmail,
                        p.TicketType,
                        p.Quantity,
                        p.CreatedAt,
                        // Joining via Ticket to get the Concert Title efficiently
                        ConcertTitle = _context.Tickets
                            .Where(t => t.PaymentId == p.PaymentId)
                            .Select(t => t.Concert.ConcertTitle)
                            .FirstOrDefault() ?? "Unknown Event"
                    })
                    .ToListAsync();

                return Ok(new { 
                    totalRevenue, 
                    totalTickets, 
                    recentPurchases,
                    totalPages = (int)Math.Ceiling((double)totalPurchasesCount / pageSize),
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Stats error", error = ex.Message });
            }
        }

        // --- 1. USER MANAGEMENT ---
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Name, u.Email, u.Role, u.IsSuspended })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("toggle-suspension/{email}")]
        public async Task<IActionResult> ToggleSuspension(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound("User not found");

            user.IsSuspended = !user.IsSuspended;
            await _context.SaveChangesAsync();
            return Ok(new { isSuspended = user.IsSuspended });
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

        // --- 2. ADD NEW CONCERT (Fixed Syntax) ---
        [HttpPost("add-concert")]
        public async Task<IActionResult> AddConcert([FromBody] Concert concert)
        {
            try
            {
                if (concert == null) return BadRequest("Invalid concert data");
                
                concert.ConcertId = 0; // Ensure DB generates ID
                _context.Concerts.Add(concert);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Concert added successfully!", concert });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.Message });
            }
        }

        // --- 3. DELETE CONCERT ---
        [HttpDelete("delete-concert/{id}")]
        public async Task<IActionResult> DeleteConcert(int id)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();

            _context.Concerts.Remove(concert);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }

        // --- 4. UPDATE CONCERT (Full Field Mapping) ---
        [HttpPut("update-concert/{id}")]
        public async Task<IActionResult> UpdateConcert(int id, [FromBody] Concert updated)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();

            concert.ConcertTitle = updated.ConcertTitle;
            concert.Venue = updated.Venue;
            concert.Date = updated.Date;
            concert.ImageUrl = updated.ImageUrl;
            concert.RegularPrice = updated.RegularPrice;
            concert.VipPrice = updated.VipPrice;
            concert.RegularStripeId = updated.RegularStripeId;
            concert.VipStripeId = updated.VipStripeId;
            concert.IsSoldOut = updated.IsSoldOut;

            await _context.SaveChangesAsync();
            return Ok(concert);
        }
    }

    public class RoleUpdateDto
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }
}