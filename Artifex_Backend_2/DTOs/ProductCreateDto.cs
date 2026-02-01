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

        public List<int> CategoryIds { get; set; } = new List<int>();
        public int StockQuantity { get; set; } = 1;

        public string StockStatus { get; set; } = "In Stock";

        public string? Tags { get; set; }

        public string? TutorialLink { get; set; }

        // This captures the files sent from React
        public List<IFormFile>? Images { get; set; }
        public bool AllowColorCustomization { get; set; } = false;
        public List<string>? ColorOptions { get; set; }

        public bool AllowSizeCustomization { get; set; } = false;
        public List<string>? SizeOptions { get; set; }
    }
}
