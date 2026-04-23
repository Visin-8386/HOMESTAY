using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebHS.Data;
using WebHS.Models;
using WebHS.ViewModels;

namespace WebHS.Services
{
    public interface IMessagingService
    {
        Task<List<ConversationViewModel>> GetUserConversationsAsync(string userId);
        Task<ConversationViewModel?> GetConversationAsync(int conversationId, string currentUserId);
        Task<List<MessageViewModel>> GetConversationMessagesAsync(int conversationId, string currentUserId, int page = 1, int pageSize = 50);
        Task<MessageViewModel?> SendMessageAsync(string senderId, SendMessageViewModel model);
        Task<bool> MarkMessageAsReadAsync(int messageId, string userId);
        Task<bool> MarkConversationAsReadAsync(int conversationId, string userId);
        Task<int> GetUnreadMessageCountAsync(string userId);
        Task<ConversationViewModel?> GetOrCreateConversationAsync(string user1Id, string user2Id, int? homestayId = null, int? bookingId = null);
        Task<List<UserSearchResultViewModel>> SearchUsersAsync(string searchTerm, string currentUserId);
        Task<bool> CanUserMessageAsync(string fromUserId, string toUserId);
        Task<bool> DeleteMessageAsync(int messageId, string userId);
        Task<bool> ArchiveConversationAsync(int conversationId, string userId);        Task<bool> RequestAdminMessageAsync(string requesterId, string message);
        Task<List<UserNotification>> GetAdminMessageRequestsAsync();
        Task<int> GetPendingAdminRequestsCountAsync();
        Task<bool> AcceptAdminMessageRequestAsync(int notificationId, string adminId);
        Task<bool> DeclineAdminMessageRequestAsync(int notificationId, string adminId);
        Task<ConversationViewModel?> GetOrCreateHostConversationAsync(string userId, string hostId, int homestayId);
    }

    public class MessagingService : IMessagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<MessagingService> _logger;

        public MessagingService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IFileUploadService fileUploadService,
            ILogger<MessagingService> logger)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<List<ConversationViewModel>> GetUserConversationsAsync(string userId)
        {
            var conversations = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.LastMessageSender)
                .Include(c => c.Homestay)
                .Include(c => c.Booking)
                .Where(c => (c.User1Id == userId || c.User2Id == userId) && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            var result = new List<ConversationViewModel>();

            foreach (var conversation in conversations)
            {
                var otherUser = conversation.GetOtherParticipant(userId);
                var otherUserRoles = await _userManager.GetRolesAsync(otherUser);
                
                // Count unread messages
                var conversationUnreadCount = await _context.Messages
                    .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted &&
                               ((m.SenderId == conversation.User1Id && m.ReceiverId == conversation.User2Id) ||
                                (m.SenderId == conversation.User2Id && m.ReceiverId == conversation.User1Id)))
                    .CountAsync();

                var subject = GenerateConversationSubject(conversation);

                result.Add(new ConversationViewModel
                {
                    Id = conversation.Id,
                    ParticipantId = otherUser.Id,
                    ParticipantName = $"{otherUser.FirstName} {otherUser.LastName}",
                    ParticipantAvatar = GetUserAvatar(otherUser),
                    ParticipantRole = otherUserRoles.FirstOrDefault() ?? "User",
                    LastMessageAt = conversation.LastMessageAt,
                    LastMessage = conversation.LastMessage,
                    LastMessageSenderId = conversation.LastMessageSenderId,
                    HasUnreadMessages = conversationUnreadCount > 0,
                    UnreadCount = conversationUnreadCount,
                    HomestayId = conversation.HomestayId,
                    HomestayName = conversation.Homestay?.Name,
                    BookingId = conversation.BookingId,
                    Subject = subject
                });
            }

            return result;
        }

        public async Task<ConversationViewModel?> GetConversationAsync(int conversationId, string currentUserId)
        {
            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Homestay)
                .Include(c => c.Booking)
                .FirstOrDefaultAsync(c => c.Id == conversationId && 
                    (c.User1Id == currentUserId || c.User2Id == currentUserId));

            if (conversation == null) return null;

            var otherUser = conversation.GetOtherParticipant(currentUserId);
            var otherUserRoles = await _userManager.GetRolesAsync(otherUser);

            var unreadCount = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead && !m.IsDeleted)
                .CountAsync();

            return new ConversationViewModel
            {
                Id = conversation.Id,
                ParticipantId = otherUser.Id,
                ParticipantName = $"{otherUser.FirstName} {otherUser.LastName}",
                ParticipantAvatar = GetUserAvatar(otherUser),
                ParticipantRole = otherUserRoles.FirstOrDefault() ?? "User",
                LastMessageAt = conversation.LastMessageAt,
                LastMessage = conversation.LastMessage,
                HasUnreadMessages = unreadCount > 0,
                UnreadCount = unreadCount,
                HomestayId = conversation.HomestayId,
                HomestayName = conversation.Homestay?.Name,
                BookingId = conversation.BookingId,
                Subject = GenerateConversationSubject(conversation)
            };
        }

        public async Task<List<MessageViewModel>> GetConversationMessagesAsync(int conversationId, string currentUserId, int page = 1, int pageSize = 50)
        {
            // Verify user has access to this conversation
            var hasAccess = await _context.Conversations
                .AnyAsync(c => c.Id == conversationId && (c.User1Id == currentUserId || c.User2Id == currentUserId));

            if (!hasAccess) return new List<MessageViewModel>();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Homestay)
                .Include(m => m.Booking)
                .Where(m => !m.IsDeleted && m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                SenderAvatar = GetUserAvatar(m.Sender),
                ReceiverId = m.ReceiverId,
                ReceiverName = $"{m.Receiver.FirstName} {m.Receiver.LastName}",
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                Type = m.Type,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentFileName = m.AttachmentFileName,
                IsSentByCurrentUser = m.SenderId == currentUserId,
                HomestayId = m.HomestayId,
                HomestayName = m.Homestay?.Name,
                BookingId = m.BookingId
            }).Reverse().ToList(); // Reverse to show oldest first
        }

        public async Task<MessageViewModel?> SendMessageAsync(string senderId, SendMessageViewModel model)
        {
            // Verify users can message each other
            if (!await CanUserMessageAsync(senderId, model.ReceiverId))
            {
                _logger.LogWarning("User {SenderId} attempted to message {ReceiverId} without permission", senderId, model.ReceiverId);
                return null;
            }

            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(senderId, model.ReceiverId, model.HomestayId, model.BookingId);
            if (conversation == null) return null;

            string? attachmentUrl = null;
            string? attachmentFileName = null;

            // Handle file attachment
            if (model.Attachment != null)
            {
                var uploadResult = await _fileUploadService.UploadImageAsync(model.Attachment, "messages");
                if (!string.IsNullOrEmpty(uploadResult))
                {
                    attachmentUrl = uploadResult;
                    attachmentFileName = model.Attachment.FileName;
                    model.Type = GetMessageTypeFromFile(model.Attachment);
                }
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = model.ReceiverId,
                Content = model.Content,
                SentAt = DateTime.UtcNow,
                Type = model.Type,
                AttachmentUrl = attachmentUrl,
                AttachmentFileName = attachmentFileName,
                HomestayId = model.HomestayId,
                BookingId = model.BookingId,
                ConversationId = conversation.Id
            };

            _context.Messages.Add(message);

            // Update conversation
            var existingConversation = await _context.Conversations.FindAsync(conversation.Id);
            if (existingConversation != null)
            {
                existingConversation.LastMessage = model.Content;
                existingConversation.LastMessageAt = DateTime.UtcNow;
                existingConversation.LastMessageSenderId = senderId;
            }

            await _context.SaveChangesAsync();

            // Return message view model
            var sender = await _userManager.FindByIdAsync(senderId);
            var receiver = await _userManager.FindByIdAsync(model.ReceiverId);

            return new MessageViewModel
            {
                Id = message.Id,
                SenderId = senderId,
                SenderName = $"{sender?.FirstName} {sender?.LastName}",
                SenderAvatar = GetUserAvatar(sender),
                ReceiverId = model.ReceiverId,
                ReceiverName = $"{receiver?.FirstName} {receiver?.LastName}",
                Content = message.Content,
                SentAt = message.SentAt,
                Type = message.Type,
                AttachmentUrl = attachmentUrl,
                AttachmentFileName = attachmentFileName,
                IsSentByCurrentUser = true,
                HomestayId = model.HomestayId,
                BookingId = model.BookingId
            };
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);

            if (message == null || message.IsRead) return false;

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkConversationAsReadAsync(int conversationId, string userId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.ReceiverId == userId && !m.IsRead && !m.IsDeleted)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            if (messages.Any())
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted)
                .CountAsync();
        }

        public async Task<ConversationViewModel?> GetOrCreateConversationAsync(string user1Id, string user2Id, int? homestayId = null, int? bookingId = null)
        {
            // Ensure consistent ordering of user IDs
            var orderedUser1Id = string.Compare(user1Id, user2Id, StringComparison.Ordinal) < 0 ? user1Id : user2Id;
            var orderedUser2Id = string.Compare(user1Id, user2Id, StringComparison.Ordinal) < 0 ? user2Id : user1Id;

            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.User1Id == orderedUser1Id && c.User2Id == orderedUser2Id);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    User1Id = orderedUser1Id,
                    User2Id = orderedUser2Id,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    HomestayId = homestayId,
                    BookingId = bookingId
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                // Reload with navigation properties
                conversation = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .FirstAsync(c => c.Id == conversation.Id);
            }

            var otherUser = conversation.GetOtherParticipant(user1Id);
            var otherUserRoles = await _userManager.GetRolesAsync(otherUser);

            return new ConversationViewModel
            {
                Id = conversation.Id,
                ParticipantId = otherUser.Id,
                ParticipantName = $"{otherUser.FirstName} {otherUser.LastName}",
                ParticipantRole = otherUserRoles.FirstOrDefault() ?? "User",
                LastMessageAt = conversation.LastMessageAt,
                HomestayId = conversation.HomestayId,
                BookingId = conversation.BookingId
            };
        }

        public async Task<List<UserSearchResultViewModel>> SearchUsersAsync(string searchTerm, string currentUserId)
        {
            var users = await _context.Users
                .Where(u => u.Id != currentUserId && u.IsActive &&
                    (u.FirstName.Contains(searchTerm) || 
                     u.LastName.Contains(searchTerm) || 
                     (u.Email != null && u.Email.Contains(searchTerm))))
                .Take(10)
                .ToListAsync();

            var result = new List<UserSearchResultViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var canMessage = await CanUserMessageAsync(currentUserId, user.Id);

                result.Add(new UserSearchResultViewModel
                {
                    Id = user.Id,
                    Name = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "User",
                    Avatar = GetUserAvatar(user),
                    CanMessage = canMessage
                });
            }

            return result;
        }

        public async Task<bool> CanUserMessageAsync(string fromUserId, string toUserId)
        {
            var fromUser = await _userManager.FindByIdAsync(fromUserId);
            var toUser = await _userManager.FindByIdAsync(toUserId);

            if (fromUser == null || toUser == null || !fromUser.IsActive || !toUser.IsActive)
                return false;

            var fromUserRoles = await _userManager.GetRolesAsync(fromUser);
            var toUserRoles = await _userManager.GetRolesAsync(toUser);

            // Admin can message anyone
            if (fromUserRoles.Contains("Admin")) return true;

            // Users can message Hosts and Admins
            if (fromUserRoles.Contains("User") && (toUserRoles.Contains("Host") || toUserRoles.Contains("Admin")))
                return true;

            // Hosts can message Users and Admins
            if (fromUserRoles.Contains("Host") && (toUserRoles.Contains("User") || toUserRoles.Contains("Admin")))
                return true;

            // Check if they have a business relationship (booking, homestay)
            var hasBusinessRelationship = await _context.Bookings
                .AnyAsync(b => (b.UserId == fromUserId && b.Homestay.HostId == toUserId) ||
                              (b.UserId == toUserId && b.Homestay.HostId == fromUserId));

            return hasBusinessRelationship;
        }

        public async Task<bool> DeleteMessageAsync(int messageId, string userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);

            if (message == null) return false;

            message.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveConversationAsync(int conversationId, string userId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && 
                    (c.User1Id == userId || c.User2Id == userId));

            if (conversation == null) return false;

            conversation.IsArchived = true;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetUserAvatar(User? user)
        {
            if (user == null) return "/images/default-avatar.jpg";
            
            // Return user avatar if available, otherwise default
            return !string.IsNullOrEmpty(user.ProfilePicture) 
                ? user.ProfilePicture 
                : "/images/default-avatar.jpg";
        }

        private string GenerateConversationSubject(Conversation conversation)
        {
            if (conversation.BookingId.HasValue)
                return $"Về booking #{conversation.BookingId}";
            
            if (conversation.HomestayId.HasValue && conversation.Homestay != null)
                return $"Về homestay: {conversation.Homestay.Name}";
            
            return "Trò chuyện";
        }

        private MessageType GetMessageTypeFromFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            
            return imageExtensions.Contains(extension) ? MessageType.Image : MessageType.File;
        }

        public async Task<bool> RequestAdminMessageAsync(string requesterId, string message)
        {
            try
            {
                var requester = await _userManager.FindByIdAsync(requesterId);
                if (requester == null) return false;

                // Get all admin users
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                
                // Create notification for all admins
                foreach (var admin in adminUsers)
                {
                    var notification = new UserNotification
                    {
                        UserId = admin.Id,
                        Type = "message_request",
                        Message = $"{requester.FullName} ({requester.Email}) muốn nhắn tin với Admin: \"{message}\"",
                        RequesterId = requesterId,
                        RequesterName = requester.FullName,
                        RequesterEmail = requester.Email,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.UserNotifications.Add(notification);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)            {
                _logger.LogError(ex, "Error requesting admin message for user {UserId}", requesterId);
                return false;
            }
        }        public async Task<List<UserNotification>> GetAdminMessageRequestsAsync()
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.Type == "message_request")
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100) // Limit to 100 most recent requests to improve performance
                    .AsNoTracking() // Don't track changes to improve performance
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting admin message requests: {ex.Message}", ex);
            }
        }

        public async Task<int> GetPendingAdminRequestsCountAsync()
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.Type == "message_request" && !n.IsAccepted)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending admin requests count");
                return 0;
            }
        }

        public async Task<bool> AcceptAdminMessageRequestAsync(int notificationId, string adminId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == adminId);
                
                if (notification == null || notification.RequesterId == null) return false;

                // Mark this notification as accepted
                notification.IsAccepted = true;
                notification.AcceptedAt = DateTime.Now;
                notification.AcceptedBy = adminId;
                notification.IsRead = true;

                // Remove other admin notifications for the same request
                var otherNotifications = await _context.UserNotifications
                    .Where(n => n.Type == "message_request" 
                               && n.RequesterId == notification.RequesterId 
                               && n.Id != notificationId
                               && !n.IsAccepted)
                    .ToListAsync();

                foreach (var otherNotif in otherNotifications)
                {
                    otherNotif.IsRead = true;
                    otherNotif.Message = "Yêu cầu này đã được xử lý bởi admin khác.";
                }

                // Create conversation between requester and admin
                var conversation = await GetOrCreateConversationAsync(notification.RequesterId, adminId);
                if (conversation != null)
                {
                    notification.ConversationId = conversation.Id;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting admin message request {NotificationId}", notificationId);
                return false;
            }
        }

        public async Task<bool> DeclineAdminMessageRequestAsync(int notificationId, string adminId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == adminId);
                
                if (notification == null) return false;

                notification.IsRead = true;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining admin message request {NotificationId}", notificationId);
                return false;
            }
        }

        public async Task<ConversationViewModel?> GetOrCreateHostConversationAsync(string userId, string hostId, int homestayId)
        {
            try
            {
                // Check if conversation already exists
                var existingConversation = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.Homestay)
                    .FirstOrDefaultAsync(c => c.HomestayId == homestayId &&
                        ((c.User1Id == userId && c.User2Id == hostId) ||
                         (c.User1Id == hostId && c.User2Id == userId)));

                if (existingConversation != null)
                {
                    return await MapToConversationViewModel(existingConversation, userId);
                }

                // Create new conversation
                var conversation = new Conversation
                {
                    User1Id = userId,
                    User2Id = hostId,
                    HomestayId = homestayId,
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                // Reload with includes
                var newConversation = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.Homestay)
                    .FirstOrDefaultAsync(c => c.Id == conversation.Id);

                return newConversation != null ? await MapToConversationViewModel(newConversation, userId) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating host conversation for user {UserId}, host {HostId}, homestay {HomestayId}", 
                    userId, hostId, homestayId);
                return null;
            }
        }

        private async Task<ConversationViewModel> MapToConversationViewModel(Conversation conversation, string currentUserId)
        {
            var otherUser = conversation.GetOtherParticipant(currentUserId);
            var otherUserRoles = await _userManager.GetRolesAsync(otherUser);

            var unreadCount = await _context.Messages
                .Where(m => m.ConversationId == conversation.Id && m.ReceiverId == currentUserId && !m.IsRead && !m.IsDeleted)
                .CountAsync();

            return new ConversationViewModel
            {
                Id = conversation.Id,
                ParticipantId = otherUser.Id,
                ParticipantName = $"{otherUser.FirstName} {otherUser.LastName}",
                ParticipantAvatar = GetUserAvatar(otherUser),
                ParticipantRole = otherUserRoles.FirstOrDefault() ?? "User",
                LastMessageAt = conversation.LastMessageAt,
                LastMessage = conversation.LastMessage,
                LastMessageSenderId = conversation.LastMessageSenderId,
                HasUnreadMessages = unreadCount > 0,
                UnreadCount = unreadCount,
                HomestayId = conversation.HomestayId,
                HomestayName = conversation.Homestay?.Name,
                BookingId = conversation.BookingId,
                Subject = GenerateConversationSubject(conversation)
            };
        }
    }
}
