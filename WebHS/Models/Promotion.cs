using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public PromotionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Validity
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }

        // Usage limits
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        // Foreign key to creator
        public string? CreatedByUserId { get; set; }
        // Navigation property to the user who created this promotion
        public virtual WebHSUser? CreatedByUser { get; set; }

        // TEMPORARILY DISABLED: User-specific promotions to avoid UserId1 conflicts
        // We can implement this feature later with a separate UserPromotion junction table
        // public string? UserId { get; set; }
        // public virtual WebHSUser? User { get; set; }

        // All bookings that used this promotion
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
