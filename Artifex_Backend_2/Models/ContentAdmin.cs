using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Artifex_Backend_2.Models
{
    public class ContentAdmin
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        public string FullName { get; set; }
        public string Department { get; set; } // e.g. "Blog", "Tutorials"
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}