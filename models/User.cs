using System.ComponentModel.DataAnnotations;

namespace Concert_Backend.Models {
    public class User {
        [Key]
        public string Email { get; set; } = null!; // Acts as the ID
        
        public string Name { get; set; } = null!;
        
        public string PasswordHash { get; set; } = null!;
        
        public string Role { get; set; } = "Customer"; // "Admin" or "Customer"
        
        public string? PhoneNumber { get; set; }

        // Relationship: One user can have many purchases
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}