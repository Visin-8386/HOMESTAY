using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Added for ICollection

namespace WebHS.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}"; // Reinstated FullName property

        [StringLength(500)]
        public string? Bio { get; set; } // Kept from original User.cs

        public string? ProfilePicture { get; set; } // Kept from original User.cs (UserFixed.cs had ProfilePictureUrl)

        [StringLength(200)]
        public string? Address { get; set; } // Kept from original User.cs        [Required]
        [StringLength(20)]
        public new string PhoneNumber { get; set; } = string.Empty; // Added 'new' keyword, kept from UserFixed.cs
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties - ONE-TO-MANY relationships
        public virtual ICollection<Homestay> Homestays { get; set; } = new List<Homestay>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
