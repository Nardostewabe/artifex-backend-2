using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Artifex_Backend_2.DTOs
{
    public class CategoryCreateDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public IFormFile Image { get; set; } 
    }
}