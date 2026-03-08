#nullable enable
using System.ComponentModel.DataAnnotations;

namespace Concert_Backend.Models {
    public class User {
        [Key]
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = "Customer";
        public string? PhoneNumber { get; set; }

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public string? ResetCode { get; set; }
        public DateTime? ResetCodeExpiry { get; set; }
        public bool IsSuspended { get; set; } = false;
    }
}