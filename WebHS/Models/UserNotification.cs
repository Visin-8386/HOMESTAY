using System.ComponentModel.DataAnnotations;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class UserNotification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Type { get; set; } = "info"; // info, success, warning, danger, message_request
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // For message requests
        public string? RequesterId { get; set; }
        public string? RequesterName { get; set; }
        public string? RequesterEmail { get; set; }
        public int? ConversationId { get; set; }
        public bool IsAccepted { get; set; } = false;
        public DateTime? AcceptedAt { get; set; }
        public string? AcceptedBy { get; set; }
        
        // Navigation property
        public virtual WebHSUser User { get; set; } = null!;
    }
}
