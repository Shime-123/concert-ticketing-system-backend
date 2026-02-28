using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StripeController(IConfiguration configuration)
        {
            _configuration = configuration;
            // Best Practice: Load Key once
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
                SuccessUrl = domain + "/success",
                CancelUrl = domain + "/cancel",
                
                // --- UPDATED METADATA ---
                // We add Artist and Venue here so the Webhook and EmailService can use them
                Metadata = new Dictionary<string, string>
                {
                    { "ticketType", request.TicketType },
                    { "quantity", request.Quantity.ToString() },
                    { "artist", request.Artist },   // Captured from the React Modal
                    { "venue", request.Venue }      // Captured from the React Modal
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }
    }

    // --- UPDATED DTO ---
    public class CheckoutRequest
    {
        public string PriceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string TicketType { get; set; } = string.Empty;
        
        // Added these to match your new React Frontend fields
        public string Artist { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
    }
}