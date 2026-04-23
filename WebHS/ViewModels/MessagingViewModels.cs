using WebHS.Models;

namespace WebHS.ViewModels
{
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public MessageType Type { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentFileName { get; set; }
        public bool IsSentByCurrentUser { get; set; }
        
        // Related entities
        public int? HomestayId { get; set; }
        public string? HomestayName { get; set; }
        public int? BookingId { get; set; }
    }

    public class ConversationViewModel
    {
        public int Id { get; set; }
        public string ParticipantId { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantAvatar { get; set; } = string.Empty;
        public string ParticipantRole { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public string? LastMessage { get; set; }
        public string? LastMessageSenderId { get; set; }
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
        
        // Related entities
        public int? HomestayId { get; set; }
        public string? HomestayName { get; set; }
        public int? BookingId { get; set; }
        public string? Subject { get; set; } // Generated subject based on context
    }

    public class MessagingIndexViewModel
    {
        public List<ConversationViewModel> Conversations { get; set; } = new();
        public ConversationViewModel? ActiveConversation { get; set; }
        public List<MessageViewModel> Messages { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public bool HasUnreadMessages { get; set; }
        public int TotalUnreadCount { get; set; }
    }

    public class SendMessageViewModel
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public IFormFile? Attachment { get; set; }
        
        // Context (optional)
        public int? HomestayId { get; set; }
        public int? BookingId { get; set; }
        public int? ConversationId { get; set; }
    }

    public class StartConversationViewModel
    {
        public string WithUserId { get; set; } = string.Empty;
        public string WithUserName { get; set; } = string.Empty;
        public string WithUserRole { get; set; } = string.Empty;
        public string InitialMessage { get; set; } = string.Empty;
        
        // Context (optional)
        public int? HomestayId { get; set; }
        public string? HomestayName { get; set; }
        public int? BookingId { get; set; }
        public string? Subject { get; set; }
    }

    public class UserSearchResultViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool CanMessage { get; set; } = true;
    }
}
