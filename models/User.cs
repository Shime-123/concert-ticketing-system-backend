using System.ComponentModel.DataAnnotations;

namespace Concert_Backend.Models {
    public class User {
        [Key]
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}