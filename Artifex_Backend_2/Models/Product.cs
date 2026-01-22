using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Artifex_Backend_2.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public Guid SellerId { get; set; }

        [ForeignKey("SellerId")]
        [JsonIgnore]
        public virtual Seller Seller { get; set; }

       

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

        public int StockQuantity { get; set; } = 1;
        public string StockStatus { get; set; } = "In Stock";
        public int OrderCount { get; set; } = 0;
        public bool IsTrending { get; set; } = false;
        public string? Tags { get; set; }
        public string? TutorialLink { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}