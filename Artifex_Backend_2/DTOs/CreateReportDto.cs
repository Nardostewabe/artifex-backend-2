using System.ComponentModel.DataAnnotations;

namespace Artifex_Backend_2.DTOs
{
    public class CreateReportDto
    {
        [Required]
        public string Reason { get; set; } // e.g., "Scam", "Inappropriate Content"

        [Required]
        public string Description { get; set; } // Detailed explanation from the user
    }
}