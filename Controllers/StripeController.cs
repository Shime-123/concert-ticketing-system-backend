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
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
        {
            var domain = "http://localhost:3000"; // Your React URL

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = request.PriceId, // The "price_..." ID from your React tickets array
                        Quantity = request.Quantity,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/success",
                CancelUrl = domain + "/cancel",
                // Crucial: Metadata allows the Webhook to see the ticket info later
                Metadata = new Dictionary<string, string>
                {
                    { "ticketType", request.TicketType },
                    { "quantity", request.Quantity.ToString() }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }
    }

    public class CheckoutRequest
    {
        public string PriceId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string TicketType { get; set; } = string.Empty;
    }
}