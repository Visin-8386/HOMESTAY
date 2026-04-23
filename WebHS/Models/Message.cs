using System.ComponentModel.DataAnnotations;

namespace WebHS.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public MessageType Type { get; set; } = MessageType.Text;

        public string? AttachmentUrl { get; set; }

        public string? AttachmentFileName { get; set; }

        // Navigation properties
        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;

        // Related entity (optional)
        public int? HomestayId { get; set; }
        public Homestay? Homestay { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        // Conversation reference
        public int? ConversationId { get; set; }
        public Conversation? Conversation { get; set; }
    }

    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2,
        System = 3 // For automated messages
    }
}
