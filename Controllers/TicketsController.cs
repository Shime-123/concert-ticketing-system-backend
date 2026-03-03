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

    // We join the Ticket table with Purchases to get the TicketType saved during checkout
    var tickets = await _context.Tickets
        .Include(t => t.Concert)
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
            Time = t.Concert.Date.ToString("hh:mm tt"),
            // 🚀 THE KEY ADDITION: Get the type and quantity from the related Purchase
            TicketType = _context.Purchases
                .Where(p => p.PaymentId == t.PaymentId)
                .Select(p => p.TicketType)
                .FirstOrDefault() ?? "Regular",
            Quantity = _context.Purchases
                .Where(p => p.PaymentId == t.PaymentId)
                .Select(p => p.Quantity)
                .FirstOrDefault()
        })
        .ToListAsync();

    return Ok(tickets);
}
    }
}