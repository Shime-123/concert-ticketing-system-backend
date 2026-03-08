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
public async Task<IActionResult> GetDashboardStats([FromQuery] int page = 1)
{
    try
    {
        // Settings
        const int pageSize = 5;
        
        // 1. Calculate Overall Totals
        var totalRevenue = await _context.Tickets.SumAsync(t => (double?)t.Price) ?? 0;
        var totalTickets = await _context.Purchases.SumAsync(p => (int?)p.Quantity) ?? 0;

        // 2. Count Total Purchases (for frontend pagination buttons)
        var totalPurchasesCount = await _context.Purchases.CountAsync();

        // 3. Fetch Paginated Recent Purchases
        var recentPurchases = await _context.Purchases
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize) // Skip previous pages
            .Take(pageSize)              // Take only 5
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

        // --- 2. ADD NEW CONCERT ---
[HttpPost("add-concert")]
public async Task<IActionResult> AddConcert([FromBody] Concert concert)
{
    try
    {
        if (concert == null) return BadRequest("Invalid concert data");
        
        // Ensure the ID is handled by the DB, not the incoming JSON
        concert.ConcertId = 0; 

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
            try
            {
                var concert = await _context.Concerts.FindAsync(id);
                if (concert == null) return NotFound("Concert not found");

                _context.Concerts.Remove(concert);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Concert deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting concert", error = ex.Message });
            }
        }

        // --- 4. EDIT CONCERT (FIXED: Added IsSoldOut & Stripe IDs) ---
        [HttpPut("update-concert/{id}")]
        public async Task<IActionResult> UpdateConcert(int id, [FromBody] Concert updatedConcert)
        {
            try 
            {
                var concert = await _context.Concerts.FindAsync(id);
                if (concert == null) return NotFound(new { message = "Concert not found" });

                // Map all fields correctly
                concert.ConcertTitle = updatedConcert.ConcertTitle;
                concert.Venue = updatedConcert.Venue;
                concert.Date = updatedConcert.Date;
                concert.ImageUrl = updatedConcert.ImageUrl;
                
                // Pricing
                concert.RegularPrice = updatedConcert.RegularPrice;
                concert.VipPrice = updatedConcert.VipPrice;

                // Stripe IDs (Crucial for payment flow)
                concert.RegularStripeId = updatedConcert.RegularStripeId;
                concert.VipStripeId = updatedConcert.VipStripeId;

                // SOLD OUT LOGIC (This was the missing piece!)
                concert.IsSoldOut = updatedConcert.IsSoldOut;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Updated successfully!", concert });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Update failed", error = ex.Message });
            }
        }

        // --- 5. EXPORT REPORT (Backend Version) ---
        [HttpGet("export")]
        public async Task<IActionResult> ExportPurchases()
        {
            try 
            {
                var purchases = await _context.Purchases.OrderByDescending(p => p.CreatedAt).ToListAsync();
                var csv = new StringBuilder();
                
                // Header row
                csv.AppendLine("PaymentId,UserEmail,TicketType,Quantity,Date");

                foreach (var p in purchases)
                {
                    // Using quotes for Email to avoid CSV breaking on special characters
                    csv.AppendLine($"{p.PaymentId},\"{p.UserEmail}\",{p.TicketType},{p.Quantity},{p.CreatedAt:yyyy-MM-dd HH:mm}");
                }

                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"ConcertSalesReport_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Export failed", error = ex.Message });
            }
        }
    }

    // Helper DTO for Role Updates
    public class RoleUpdateDto
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }
}