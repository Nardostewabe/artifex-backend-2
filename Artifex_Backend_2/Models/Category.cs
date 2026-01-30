using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Artifex_Backend_2.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        [JsonIgnore]
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}