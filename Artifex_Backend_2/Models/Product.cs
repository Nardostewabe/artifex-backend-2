using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key to your User/Seller table
        // Assuming you use IdentityUser or a custom User class
        public Guid SellerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty; // e.g., "Ceramics"

        public int StockQuantity { get; set; } = 1;

        public string StockStatus { get; set; } = "In Stock"; // Enum string

        public string? Tags { get; set; } // Comma separated string: "clay, vintage"

        public string? TutorialLink { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property: One Product has many Images
        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

    }
}
