using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Concert_Backend.Models
{
    public class Ticket
    {
        [Key]
        public Guid TicketId { get; set; } = Guid.NewGuid();
        
        public string PaymentId { get; set; } = null!;
        
        [ForeignKey("PaymentId")]
        public Purchase? Purchase { get; set; }

        public string? CustomerName { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public string Status { get; set; } = "Valid";

        public int ConcertId { get; set; }
        
        [ForeignKey("ConcertId")]
        public Concert Concert { get; set; } = null!;
    }
}