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
        // Calculate totals
        var totalRevenue = await _context.Tickets.SumAsync(t => (double?)t.Price) ?? 0;
        var totalTickets = await _context.Purchases.SumAsync(p => (int?)p.Quantity) ?? 0;

        // Fetch ALL purchases so React pagination/search works on the full set
        var recentPurchases = await _context.Purchases
            .OrderByDescending(p => p.CreatedAt)
            // .Take(10)  <-- REMOVED THIS LINE
            .Select(p => new {
                p.PaymentId,
                p.UserEmail,
                p.TicketType,
                p.Quantity,
                p.CreatedAt,
                // Join with Tickets/Concert to get the Title
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

        // --- 2. ADD NEW CONCERT ---
        [HttpPost("add-concert")]
        public async Task<IActionResult> AddConcert([FromBody] Concert concert)
        {
            try
            {
                if (concert == null) return BadRequest("Invalid concert data");

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
}