using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 50, ErrorMessage = "Number of guests must be between 1 and 50")]
        public int NumberOfGuests { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be non-negative")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Final amount must be non-negative")]
        public decimal FinalAmount { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Paid;        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign keys
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int HomestayId { get; set; }

        public int? PromotionId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual WebHSUser User { get; set; } = null!;
        
        [ForeignKey("HomestayId")]
        public virtual Homestay Homestay { get; set; } = null!;
        
        [ForeignKey("PromotionId")]
        public virtual WebHSPromotion? Promotion { get; set; }
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Review fields (merged from Review model)
        [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5")]
        public int? ReviewRating { get; set; }

        [StringLength(1000, ErrorMessage = "Review comment cannot exceed 1000 characters")]
        public string? ReviewComment { get; set; }

        public bool ReviewIsActive { get; set; } = true;
        public DateTime? ReviewCreatedAt { get; set; }
        public DateTime? ReviewUpdatedAt { get; set; }

        // Computed properties for easier access in views
        public string UserName => User != null ? $"{User.FirstName} {User.LastName}" : "";
        public string UserEmail => User?.Email ?? "";
        public string HomestayName => Homestay?.Name ?? "";
        public string HomestayLocation => Homestay != null ? $"{Homestay.City}, {Homestay.State}" : "";
        public string HostName => Homestay?.Host != null ? $"{Homestay.Host.FirstName} {Homestay.Host.LastName}" : "";
        public string HostEmail => Homestay?.Host?.Email ?? "";

        // Validation properties
        [NotMapped]
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;

        [NotMapped]
        public bool IsValidDateRange => CheckOutDate > CheckInDate && NumberOfNights >= 1; // FIXED: Minimum 1-night stay

        [NotMapped]
        public bool IsMinimumStayMet => NumberOfNights >= 1; // ADDED: Minimum stay validation

        [NotMapped]
        public bool IsInFuture => CheckInDate > DateTime.UtcNow;

        [NotMapped]
        public bool CanBeCancelled => Status == BookingStatus.Paid;

        [NotMapped]
        public bool CanBeReviewed => Status == BookingStatus.Completed && ReviewRating == null;

        // Review computed properties
        [NotMapped]
        public bool HasReview => ReviewRating.HasValue;

        [NotMapped]
        public string ReviewFormattedCreatedAt => ReviewCreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";

        [NotMapped]
        public bool ReviewCanBeEdited => ReviewCreatedAt.HasValue && (DateTime.UtcNow - ReviewCreatedAt.Value).TotalHours <= 24;

        [NotMapped]
        public string ReviewRatingStars => ReviewRating.HasValue ? 
            new string('★', ReviewRating.Value) + new string('☆', 5 - ReviewRating.Value) : "";        [NotMapped]
        public string ReviewUserFullName => User != null ? $"{User.FirstName} {User.LastName}" : "";
    }public enum BookingStatus
    {
        Pending = 0,    // Chờ thanh toán
        Paid = 1,       // Đã thanh toán
        Cancelled = 2,  // Đã hủy
        Completed = 3   // Đã hoàn thành
    }
}
