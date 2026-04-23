using System.ComponentModel.DataAnnotations;

namespace WebHS.Models
{
    public class Amenity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<HomestayAmenity> HomestayAmenities { get; set; } = new List<HomestayAmenity>();
    }
}
