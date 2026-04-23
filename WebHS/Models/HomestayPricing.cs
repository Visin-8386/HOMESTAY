using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHS.Models
{
    public class HomestayPricing
    {
        public int Id { get; set; }

        [Required]
        public int HomestayId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price per night must be between 0.01 and 999,999.99")]
        public decimal PricePerNight { get; set; }

        [StringLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("HomestayId")]
        public virtual Homestay Homestay { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public bool IsInPast => Date < DateTime.Today;

        [NotMapped]
        public bool IsToday => Date == DateTime.Today;

        [NotMapped]
        public bool IsInFuture => Date > DateTime.Today;

        [NotMapped]
        public string FormattedDate => Date.ToString("dd/MM/yyyy");

        [NotMapped]
        public string FormattedPrice => PricePerNight.ToString("N0") + " VND";
    }
}
