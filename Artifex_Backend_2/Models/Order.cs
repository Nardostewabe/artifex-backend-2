using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public Guid BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        
        public virtual Customer Buyer { get; set; }
        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal TotalPrice { get; set; } // Price * Quantity at time of purchase
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
    }
}
