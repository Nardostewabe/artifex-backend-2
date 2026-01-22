using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Artifex_Backend_2.DTOs
    {
        public class ProductUpdateDto
        {
            [Required]
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public List<int>? CategoryIds { get; set; }
            public int StockQuantity { get; set; }
            public string StockStatus { get; set; }
            public string? Tags { get; set; }
            public string? TutorialLink { get; set; }

            // List of Image IDs that (ALREADY EXIST) and should be KEPT. 
            // Any existing image NOT in this list will be DELETED.
            public List<int>? KeepImageIds { get; set; }

            // New images to upload
            public List<IFormFile>? NewImages { get; set; }
        }
    }

