using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;
using Concert_Backend.Models;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetMyTickets([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Email is required");

            // We look through Purchases linked to this user email, then include the Tickets
            var tickets = await _context.Tickets
                .Include(t => t.Purchase)
                .Where(t => t.Purchase.UserEmail == email)
                .Select(t => new {
                    t.TicketId,
                    t.PaymentId,
                    t.CustomerName,
                    t.Price,
                    t.Status,
                    // If you have artist/venue in metadata or models, add them here:
                    Artist = "Ethiopian Concert", 
                    Date = DateTime.Now.ToShortDateString()
                })
                .ToListAsync();

            return Ok(tickets);
        }
    }
}