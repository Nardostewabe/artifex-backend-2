using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class Customer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        [MaxLength(200)]
        public string ? FullName { get; set; }
        public string ? ProfilePictureUrl { get; set; }
        public string ? ShippingAddress { get; set; }
        public string ? PhoneNumber { get; set; }
       
    }
}
