using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Concert_Backend.Data;
using Concert_Backend.Models;
using Concert_Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public WebhookController(AppDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

[HttpPost]
public async Task<IActionResult> Index()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var signature = Request.Headers["Stripe-Signature"];

    try
    {
        // FIX: Added throwOnApiVersionMismatch: false
        var stripeEvent = EventUtility.ConstructEvent(
            json, 
            signature, 
            _configuration["Stripe:WebhookSecret"], 
            throwOnApiVersionMismatch: false 
        );

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            
            // Extract Metadata safely
            session.Metadata.TryGetValue("concertId", out var cId);
            int.TryParse(cId, out int concertId);
            
            var email = session.CustomerDetails.Email;

            // 1. Create Purchase
            var purchase = new Purchase {
                PaymentId = session.Id,
                UserEmail = email,
                TicketType = session.Metadata.GetValueOrDefault("ticketType", "Regular"),
                Quantity = int.Parse(session.Metadata.GetValueOrDefault("quantity", "1")),
                CreatedAt = DateTime.UtcNow
            };

            // 2. Create Ticket
            var ticket = new Ticket {
                TicketId = Guid.NewGuid(),
                PaymentId = session.Id,
                CustomerName = session.CustomerDetails.Name,
                Price = (decimal)((session.AmountTotal ?? 0) / 100.0),
                Status = "Valid",
                ConcertId = concertId
            };

            _context.Purchases.Add(purchase);
            _context.Tickets.Add(ticket);
            
            await _context.SaveChangesAsync();
            
            // Optional: Logic to automatically set Sold Out if capacity reached
            // await CheckAndSetSoldOutStatus(concertId); 
        }

        return Ok();
    }
    catch (Exception ex)
    {
        // This will now print the REAL error if something else fails
        Console.WriteLine($"❌ Webhook Error: {ex.Message}");
        return StatusCode(500, ex.Message);
    }
}
    }
}