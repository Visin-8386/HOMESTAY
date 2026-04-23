using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHS.Models
{
    public class HomestayImage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Caption cannot exceed 200 characters")]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;
        
        [Range(0, 999, ErrorMessage = "Order must be between 0 and 999")]
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        [Required]
        public int HomestayId { get; set; }

        // Navigation property
        [ForeignKey("HomestayId")]
        public virtual Homestay Homestay { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public string FileName => Path.GetFileName(ImageUrl);

        [NotMapped]
        public string FileExtension => Path.GetExtension(ImageUrl);

        [NotMapped]
        public bool IsValidImageExtension => 
            new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }
            .Contains(FileExtension.ToLowerInvariant());
    }
}
