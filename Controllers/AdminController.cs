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
                // 1. Calculate Total Revenue (Sum of all ticket prices)
                var totalRevenue = await _context.Tickets.SumAsync(t => t.Price);

                // 2. Calculate Total Tickets Sold
                var totalTickets = await _context.Tickets.CountAsync();

                // 3. Get Recent Transactions (Join Purchase and User)
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
                    TotalTicketsSold = totalTickets,
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