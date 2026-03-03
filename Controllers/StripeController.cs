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
            var domain = "https://concert-ticketing-system-frontend.onrender.com"; 

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
                    { "quantity", request.Quantity.ToString() },
                    { "concertTitle", request.ConcertTitle },
                    { "venue", request.Venue }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return Ok(new { url = session.Url });
        }

[HttpPost("finalize")]
public async Task<IActionResult> FinalizePurchase([FromBody] FinalizeRequest request)
{
    // ... (your existing validation and session fetch)

    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // ... (your existing purchase/ticket creation)

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine("✅ Database Saved. Triggering email...");

        // FIX: Start the background task without awaiting it
        // We pass the data explicitly so it doesn't need the Controller anymore
        _ = Task.Run(async () => {
            try 
            {
                await _emailService.SendTicketEmailAsync(
                    purchase.UserEmail,
                    ticket.CustomerName,
                    purchase.TicketType,
                    purchase.Quantity,
                    ticket.TicketId.ToString(),
                    session.Metadata["concertTitle"],
                    session.Metadata["venue"]
                );
                Console.WriteLine("📧 EMAIL SENT SUCCESSFULLY.");
            }
            catch (Exception mailEx)
            {
                Console.WriteLine($"❌ BACKGROUND EMAIL FAILED: {mailEx.Message}");
            }
        });

        return Ok(new { message = "Success" });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, ex.Message);
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
                    session.Metadata["venue"]
                );
                Console.WriteLine("📧 EMAIL SENT SUCCESSFULLY.");
            }
            catch (Exception mailEx)
            {
                Console.WriteLine($"❌ EMAIL FAILED: {mailEx.Message}");
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