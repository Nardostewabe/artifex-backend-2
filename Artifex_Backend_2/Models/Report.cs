using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        public Guid ReporterId { get; set; } // The Customer who is flagging
        [ForeignKey("ReporterId")]
        public User Reporter { get; set; }

        public string TargetType { get; set; } // "Product" or "Seller"

        // ✅ NEW: Allow linking to a Product
        public int? TargetProductId { get; set; }
        [ForeignKey("TargetProductId")]
        public Product? Product { get; set; }

        // ✅ NEW: Allow linking to a Seller
        public Guid? TargetSellerId { get; set; }
        [ForeignKey("TargetSellerId")]
        public Seller? Seller { get; set; }

        public string Reason { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}