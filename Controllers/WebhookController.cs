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
        private readonly EmailService _emailService;

        public WebhookController(AppDbContext context, IConfiguration configuration, EmailService emailService)
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
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            signature,
            _configuration["Stripe:WebhookSecret"],
            throwOnApiVersionMismatch: false
        );

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return BadRequest();

            Console.WriteLine($"✅ Verified Signature! Processing session: {session.Id}");

            // 1. Extract Core Data
            var email = session.CustomerDetails?.Email ?? "unknown@test.com";
            var name = session.CustomerDetails?.Name ?? "Guest";
            
            // 2. Extract NEW Metadata (Artist and Venue)
            session.Metadata.TryGetValue("ticketType", out var ticketType);
            session.Metadata.TryGetValue("quantity", out var quantityStr);
            session.Metadata.TryGetValue("artist", out var artist); // <--- NEW
            session.Metadata.TryGetValue("venue", out var venue);   // <--- NEW
            
            ticketType ??= "General Admission";
            artist ??= "Teddy Afro"; // Fallback default
            venue ??= "Main Arena";
            int.TryParse(quantityStr ?? "1", out int quantity);

            // 3. Handle User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User { Email = email, Name = name };
                _context.Users.Add(user);
            }

            // 4. Create Purchase Record (Updated with Artist)
            var purchase = new Purchase
            {
                PaymentId = session.Id,
                UserEmail = email,
                TicketType = $"{artist} - {ticketType}", // Log which artist they bought
                Quantity = quantity,
                CreatedAt = DateTime.Now
            };
            _context.Purchases.Add(purchase);

            // 5. Create Ticket Record
            var ticket = new Ticket
            {
                TicketId = Guid.NewGuid(),
                PaymentId = session.Id,
                CustomerName = name,
                Price = (decimal)((session.AmountTotal ?? 0) / 100.0),
                Status = "Valid"
            };
            _context.Tickets.Add(ticket);

            // 6. Save to Database
            await _context.SaveChangesAsync();
            Console.WriteLine($"💾 Data saved for {artist} concert successfully!");

            // 7. Send Confirmation Email (Passing the artist and venue)
            try 
            {
                // Ensure your EmailService.SendTicketEmailAsync signature is updated to accept these!
                await _emailService.SendTicketEmailAsync(email, name, ticketType, quantity, session.Id, artist, venue);
                Console.WriteLine($"📧 {artist} ticket confirmation email sent.");
            }
            catch (Exception emailEx)
            {
                Console.WriteLine($"⚠️ Database saved, but Email failed: {emailEx.Message}");
            }
        }

        return Ok();
    }
    catch (Exception e)
    {
        Console.WriteLine($"❌ Server Error: {e.Message}");
        return StatusCode(500, e.Message);
    }
}
    }
}