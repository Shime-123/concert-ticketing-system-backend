using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using Concert_Backend.Data; 
using Concert_Backend.Models;
using Concert_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public StripeController(IConfiguration configuration, AppDbContext context, IEmailService emailService)
        {
            _configuration = configuration;
            _context = context; 
            _emailService = emailService;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

[HttpPost("create-checkout")]
public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
{
    try 
    {
        var domain = "https://concert-ticketing-system-frontend.onrender.com"; 

        var concert = await _context.Concerts.FindAsync(request.ConcertId);
        if (concert == null) return NotFound("Concert not found");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = request.PriceId, 
                    Quantity = request.Quantity,
                },
            },
            Mode = "payment",
            SuccessUrl = domain + "/success?session_id={CHECKOUT_SESSION_ID}", 
            CancelUrl = domain + "/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "concertId", request.ConcertId.ToString() },
                { "ticketType", request.TicketType },
                { "imageUrl", concert.ImageUrl ?? "" }, 
                { "quantity", request.Quantity.ToString() },
                { "concertTitle", request.ConcertTitle ?? "Concert" },
                { "venue", request.Venue ?? "Venue" }
            }
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);
        return Ok(new { url = session.Url });
    }
    catch (Exception ex)
    {
        // This line is key! It will show the REAL error in Render logs
        Console.WriteLine($"STRIPE ERROR: {ex.Message}"); 
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpPost("finalize")]
public async Task<IActionResult> FinalizePurchase([FromBody] FinalizeRequest request)
{
    if (string.IsNullOrEmpty(request.SessionId)) return BadRequest("Missing Session ID");

    var service = new SessionService();
    var session = await service.GetAsync(request.SessionId);

    // 1. Check if records already exist
    var existingPurchase = await _context.Purchases.FirstOrDefaultAsync(p => p.PaymentId == session.Id);
    var existingTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.PaymentId == session.Id);

    if (existingPurchase != null && existingTicket != null)
    {
        Console.WriteLine("♻️ Record already exists. Re-triggering email...");
        _ = Task.Run(() => SendBackgroundEmail(existingPurchase, existingTicket, session));
        
        // Return recipient details even for existing records
        return Ok(new { 
            message = "Success (Already Processed)", 
            recipientEmail = existingPurchase.UserEmail, 
            recipientName = existingTicket.CustomerName 
        });
    }

    session.Metadata.TryGetValue("concertId", out var cIdStr);
    int.TryParse(cIdStr, out int concertId);

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var utcNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        // 2. Create Purchase (Recipient Email from Stripe takes priority)
        var purchase = new Purchase
        {
            PaymentId = session.Id,
            UserEmail = session.CustomerDetails?.Email ?? request.UserEmail,
            TicketType = session.Metadata.ContainsKey("ticketType") ? session.Metadata["ticketType"] : "Regular",
            Quantity = int.Parse(session.Metadata.ContainsKey("quantity") ? session.Metadata["quantity"] : "1"),
            CreatedAt = utcNow
        };
        _context.Purchases.Add(purchase);

        // 3. Create Ticket (Recipient Name from Stripe takes priority)
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid(),
            PaymentId = session.Id,
            CustomerName = session.CustomerDetails?.Name ?? request.UserName,
            Price = (decimal)((session.AmountTotal ?? 0) / 100.0),
            Status = "Valid",
            ConcertId = concertId
        };
        _context.Tickets.Add(ticket);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine("✅ Database Saved. Triggering email...");
        _ = Task.Run(() => SendBackgroundEmail(purchase, ticket, session));

        // 4. Return the Recipient Details to the Frontend
        return Ok(new { 
            message = "Success", 
            recipientEmail = purchase.UserEmail, 
            recipientName = ticket.CustomerName 
        });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        Console.WriteLine($"❌ DATABASE ERROR: {ex.Message}");
        return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
    }
}
        
private async Task SendBackgroundEmail(Purchase purchase, Ticket ticket, Session session)
{
    try 
    {
        await _emailService.SendTicketEmailAsync(
            purchase.UserEmail,
            ticket.CustomerName,
            purchase.TicketType,
            purchase.Quantity,
            ticket.TicketId.ToString(),
            session.Metadata["concertTitle"],
            session.Metadata["venue"],
            session.Metadata["imageUrl"] // 👈 PASS THE IMAGE URL HERE
        );
        Console.WriteLine("📧 DYNAMIC TICKET SENT.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ BACKGROUND EMAIL ERROR: {ex.Message}");
    }
}
    }

    // DTOs remain the same
    public class CheckoutRequest
    {
        public string PriceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string TicketType { get; set; } = string.Empty;
        public int ConcertId { get; set; } 
        public string ConcertTitle { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    public class FinalizeRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}