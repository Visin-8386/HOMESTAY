using System.ComponentModel.DataAnnotations;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class MessageTemplate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public MessageTemplateType Type { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string HostId { get; set; } = string.Empty;

        public WebHSUser Host { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum MessageTemplateType
    {
        WelcomeMessage = 1,
        BookingConfirmation = 2,
        CheckInInstructions = 3,
        CheckOutReminder = 4,
        ThankYouMessage = 5,
        HouseRules = 6,
        LocalRecommendations = 7,
        Custom = 8
    }
}
