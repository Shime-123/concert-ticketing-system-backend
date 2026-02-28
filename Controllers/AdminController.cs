using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;

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

[HttpGet("stats")]
public async Task<IActionResult> GetDashboardStats()
{
    try
    {
        // 1. Calculate Total Revenue
        var totalRevenue = await _context.Tickets.SumAsync(t => t.Price);

        // 2. FIXED: Sum the Quantity column instead of counting rows
        // This ensures a purchase of 3 tickets counts as 3, not 1.
        var totalTickets = await _context.Purchases.SumAsync(p => p.Quantity);

        // 3. Get Recent Transactions
        var recentPurchases = await _context.Purchases
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new {
                p.PaymentId,
                p.UserEmail,
                p.TicketType,
                p.Quantity,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TotalRevenue = totalRevenue,
            TotalTicketsSold = totalTickets, // Now reflects true ticket count
            RecentPurchases = recentPurchases
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Error fetching stats", error = ex.Message });
    }
}
    }
}