using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHS.Models
{
    public class BlockedDate
    {
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters")]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        [Required]
        public int HomestayId { get; set; }

        // Navigation property
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
        public bool CanBeRemoved => Date >= DateTime.Today;
    }
}
