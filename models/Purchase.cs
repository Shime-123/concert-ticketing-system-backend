using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concert_Backend.Models {
    public class Purchase {
        [Key]
        public string PaymentId { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string TicketType { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserEmail")]
        public User User { get; set; } = null!;
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}