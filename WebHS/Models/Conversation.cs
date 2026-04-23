using System.ComponentModel.DataAnnotations;

namespace WebHS.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        [Required]
        public string User1Id { get; set; } = string.Empty;

        [Required]
        public string User2Id { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public string? LastMessage { get; set; }

        public string? LastMessageSenderId { get; set; }

        public bool IsArchived { get; set; } = false;

        // Navigation properties
        public User User1 { get; set; } = null!;
        public User User2 { get; set; } = null!;
        public User? LastMessageSender { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // Related entity (optional) - for conversation about specific homestay/booking
        public int? HomestayId { get; set; }
        public Homestay? Homestay { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        /// <summary>
        /// Get the other participant in the conversation
        /// </summary>
        public string GetOtherParticipantId(string currentUserId)
        {
            return currentUserId == User1Id ? User2Id : User1Id;
        }

        /// <summary>
        /// Get the other participant user object
        /// </summary>
        public User GetOtherParticipant(string currentUserId)
        {
            return currentUserId == User1Id ? User2 : User1;
        }
    }
}
