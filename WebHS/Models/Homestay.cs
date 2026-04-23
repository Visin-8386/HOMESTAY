using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class Homestay
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Ward { get; set; } = string.Empty; // Phường/Xã

        [StringLength(100)]
        public string? District { get; set; } = string.Empty; // Quận/Huyện

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty; // Tỉnh/Thành phố

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty; // Khu vực/Miền (Bắc/Trung/Nam)

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = "Vietnam"; // Quốc gia

        [Required]
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        public int MaxGuests { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }        [StringLength(2000)]
        public string? Rules { get; set; }

        [StringLength(50)]
        public string? YouTubeVideoId { get; set; } // ID video YouTube do host thêm

        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        
        // Tracking
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign key
        [Required]
        public string HostId { get; set; } = string.Empty;

        // Navigation properties
        public virtual WebHSUser Host { get; set; } = null!;
        public virtual ICollection<HomestayImage> Images { get; set; } = new List<HomestayImage>();
        public virtual ICollection<HomestayAmenity> HomestayAmenities { get; set; } = new List<HomestayAmenity>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<BlockedDate> BlockedDates { get; set; } = new List<BlockedDate>();
        public virtual ICollection<HomestayPricing> PricingRules { get; set; } = new List<HomestayPricing>();

        // Computed properties for reviews (now calculated from bookings)
        [NotMapped]
        public double AverageRating => Bookings.Where(b => b.ReviewRating.HasValue).Any() ? 
            Bookings.Where(b => b.ReviewRating.HasValue).Average(b => b.ReviewRating!.Value) : 0;

        [NotMapped]
        public int ReviewCount => Bookings.Count(b => b.ReviewRating.HasValue);
    }
}
