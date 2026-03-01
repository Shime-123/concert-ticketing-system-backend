using System.ComponentModel.DataAnnotations;

namespace Concert_Backend.Models
{
    public class Ticket
    {
        [Key]
        public Guid TicketId { get; set; } // Matches UNIQUEIDENTIFIER
        public string PaymentId { get; set; } = null!; // Matches NVARCHAR
        public string? CustomerName { get; set; } // Matches NVARCHAR (Nullable)
        public decimal Price { get; set; } // Matches DECIMAL(18,2)
        public string Status { get; set; } = "Valid";
        public Purchase? Purchase { get; set; }
        public int ConcertId { get; set; }
        public Concert Concert { get; set; }
    }
}