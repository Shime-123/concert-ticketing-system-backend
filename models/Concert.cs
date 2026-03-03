using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concert_Backend.Models
{
    public class Concert
    {
        public int ConcertId { get; set; }
        public string ConcertTitle { get; set; }
        public string Venue { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string ImageUrl { get; set; }

        // New Logic
        public decimal RegularPrice { get; set; }
        public string RegularStripeId { get; set; }
        
        public decimal VipPrice { get; set; }
        public string VipStripeId { get; set; }
        public bool IsSoldOut { get; set; } = false;
    }
}