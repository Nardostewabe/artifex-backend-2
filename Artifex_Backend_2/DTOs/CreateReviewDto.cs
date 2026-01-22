using System.ComponentModel.DataAnnotations;

namespace Artifex_Backend_2.DTOs
{
    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }
    }
}