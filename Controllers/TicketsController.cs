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
    if (string.IsNullOrEmpty(email)) return BadRequest("Email required");

    var tickets = await _context.Tickets
        .Include(t => t.Concert)
        // Link to Purchase table to verify ownership via Email
        .Where(t => _context.Purchases.Any(p => p.PaymentId == t.PaymentId && p.UserEmail == email))
        .Select(t => new {
            t.TicketId,
            t.PaymentId,
            t.CustomerName,
            t.Price,
            t.Status,
            ConcertTitle = t.Concert.ConcertTitle, 
            Venue = t.Concert.Venue,
            Date = t.Concert.Date.ToString("MMM dd, yyyy"),
            Time = t.Concert.Date.ToString("hh:mm tt")
        })
        .ToListAsync();

    return Ok(tickets);
}
    }
}