using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public Guid UserId { get; set; } // Link to the Vendor/User
        [ForeignKey("UserId")]
        public User User { get; set; }

        public string TxRef { get; set; } // Unique Transaction Reference
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ETB";
        public string Email { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public string? PaymentMethod { get; set; } // card, telebirr, etc.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}