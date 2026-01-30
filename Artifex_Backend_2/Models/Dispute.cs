using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Dispute
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        public Guid ComplainantId { get; set; } // User who opened the dispute
        [ForeignKey("ComplainantId")]
        public User Complainant { get; set; }

        public string Reason { get; set; } // e.g., "Item not received"
        public string Description { get; set; }
        public string Status { get; set; } = "Open"; // Open, Resolved, Closed
        public string? AdminResolution { get; set; } // Notes from admin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}