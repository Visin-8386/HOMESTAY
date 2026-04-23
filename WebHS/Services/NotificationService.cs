using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public interface INotificationService
    {
        Task SendBookingNotificationAsync(string userId, string message, string type = "info");
        Task SendEmailNotificationAsync(string email, string subject, string message);
        Task SendBulkNotificationAsync(List<string> userIds, string message, string type = "info");
    }

    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;
        private readonly ApplicationDbContext _context;

        public NotificationService(
            IEmailService emailService, 
            ILogger<NotificationService> logger,
            ApplicationDbContext context)
        {
            _emailService = emailService;
            _logger = logger;
            _context = context;
        }

        public async Task SendBookingNotificationAsync(string userId, string message, string type = "info")
        {
            try
            {
                // Store notification in database for user to see in app
                var notification = new UserNotification
                {
                    UserId = userId,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Notification sent to user {UserId}: {Message}", userId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        public async Task SendEmailNotificationAsync(string email, string subject, string message)
        {
            try
            {
                await _emailService.SendEmailAsync(email, subject, message);
                _logger.LogInformation("Email notification sent to {Email}: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification to {Email}", email);
            }
        }

        public async Task SendBulkNotificationAsync(List<string> userIds, string message, string type = "info")
        {
            try
            {
                var notifications = userIds.Select(userId => new UserNotification
                {
                    UserId = userId,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.UserNotifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk notification sent to {Count} users: {Message}", userIds.Count, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk notifications");
            }
        }
    }
}
