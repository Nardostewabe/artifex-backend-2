using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public enum UserRole
    {
       User = 0,
       Customer = 1,
       Seller = 2,
       ContentAdmin = 3,
       PlatformAdmin = 4
    }
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; } = UserRole.User;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Seller SellerProfile { get; set; }
        public Customer CustomerProfile { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public bool IsSuspended { get; set; } = false;
    }
}
