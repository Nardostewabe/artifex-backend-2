using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Seller
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        [MaxLength(200)]
        public string ? ShopName { get; set; }
        public string ? Description { get; set; }
        public string ? ShopLogo { get; set; }
        public string ? Category { get; set; }
        public string ?  ContactNumber { get; set; }
        public string ? Address { get; set; }
        public bool IsApproved { get; set; } = false;
        public bool IsSuspended { get; set; } = false;
        public bool IsDeactivated { get; set; } = false;

    }
}
