using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using Concert_Backend.Data; 
using Concert_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        // FIX 1: Declare the database context field
        private readonly AppDbContext _context;

        // FIX 2: Add AppDbContext to the constructor
        public StripeController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context; // Initialize the context
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
        {
            var domain = "http://localhost:3000"; 

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
                SuccessUrl = domain + "/success?session_id={CHECKOUT_SESSION_ID}", // Added session_id helper
                CancelUrl = domain + "/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "ticketType", request.TicketType },
                    { "quantity", request.Quantity.ToString() },
                    { "artist", request.Artist },
                    { "venue", request.Venue }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return Ok(new { url = session.Url });
        }

        [HttpPost("finalize-purchase")]
        public async Task<IActionResult> FinalizePurchase([FromBody] FinalizeRequest request)
        {
            // FIX 3: Ensure property names match your specific Purchase model. 
            // If your Purchase model uses different names (like 'Id' instead of 'PurchaseId'), change them here.
            var purchase = new Purchase
            {
                // If you get error CS0117 here, check Purchase.cs for the correct property names
                UserEmail = request.UserEmail,
                // PurchaseDate = DateTime.UtcNow // If this fails, use 'CreatedAt' if that's what's in your model
            };

            var ticket = new Ticket
            {
                TicketId = Guid.NewGuid(),
                PaymentId = request.SessionId,
                CustomerName = request.UserName,
                Price = 20.00m, 
                Status = "Valid",
                Purchase = purchase
            };

            _context.Purchases.Add(purchase);
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Database updated successfully" });
        }
    }

    public class CheckoutRequest
    {
        public string PriceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string TicketType { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
    }

    public class FinalizeRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}