using System.ComponentModel.DataAnnotations;

namespace Artifex_Backend_2.DTOs
{
    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public int StockQuantity { get; set; } = 1;

        public string StockStatus { get; set; } = "In Stock";

        public string? Tags { get; set; }

        public string? TutorialLink { get; set; }

        // This captures the files sent from React
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
