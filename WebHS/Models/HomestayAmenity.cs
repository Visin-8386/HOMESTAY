using System.ComponentModel.DataAnnotations.Schema;

namespace WebHS.Models
{
    public class HomestayAmenity
    {
        public int HomestayId { get; set; }
        public int AmenityId { get; set; }

        // Navigation properties
        [ForeignKey("HomestayId")]
        public virtual Homestay Homestay { get; set; } = null!;
        
        [ForeignKey("AmenityId")]
        public virtual Amenity Amenity { get; set; } = null!;
    }
}
