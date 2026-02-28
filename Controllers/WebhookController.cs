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
                // 1. Verify Event (with Version Mismatch bypass)
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

                    // 2. Extract Data from Metadata and Session
                    var email = session.CustomerDetails?.Email ?? "unknown@test.com";
                    var name = session.CustomerDetails?.Name ?? "Guest";
                    
                    session.Metadata.TryGetValue("ticketType", out var ticketType);
                    session.Metadata.TryGetValue("quantity", out var quantityStr);
                    
                    ticketType ??= "General Admission";
                    int.TryParse(quantityStr ?? "1", out int quantity);

                    // 3. Handle User (Find or Create)
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user == null)
                    {
                        user = new User { Email = email, Name = name };
                        _context.Users.Add(user);
                        Console.WriteLine($"New user created: {email}");
                    }

                    // 4. Create Purchase Record
                    var purchase = new Purchase
                    {
                        PaymentId = session.Id,
                        UserEmail = email,
                        TicketType = ticketType,
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
                        Price = (decimal)(session.AmountTotal / 100.0),
                        Status = "Valid"
                    };
                    _context.Tickets.Add(ticket);

                    // 6. Save to Database
                    await _context.SaveChangesAsync();
                    Console.WriteLine("💾 Data saved to SQL Server successfully!");

                    // 7. Send Confirmation Email
                    try 
                    {
                        await _emailService.SendTicketEmailAsync(
                            email,
                            name,
                            $"Your purchase of {quantity} {ticketType} ticket(s) is confirmed! Transaction ID: {session.Id}"
                        );
                        Console.WriteLine("📧 Confirmation email sent.");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"⚠️ Database saved, but Email failed: {emailEx.Message}");
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"❌ Stripe Webhook Error: {e.Message}");
                return BadRequest();
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Server Error: {e.Message}");
                // Log inner exception for database errors
                if (e.InnerException != null) Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
                return StatusCode(500, e.Message);
            }
        }
    }
}