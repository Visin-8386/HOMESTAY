using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Services
{
    public class MessageTemplateService : IMessageTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MessageTemplateService> _logger;

        public MessageTemplateService(
            ApplicationDbContext context,
            IEmailService emailService,
            UserManager<User> userManager,
            ILogger<MessageTemplateService> logger)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> ProcessTemplateAsync(int templateId, string userId, int? bookingId = null, Dictionary<string, string>? customData = null)
        {
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == templateId && mt.HostId == userId && mt.IsActive);

            if (template == null)
            {
                throw new ArgumentException("Template not found or not active");
            }

            return await ProcessTemplateContentAsync(template, userId, bookingId, customData);
        }

        public async Task<bool> SendTemplateEmailAsync(string toEmail, int templateId, string userId, int? bookingId = null, Dictionary<string, string>? customData = null)
        {
            try
            {
                var template = await _context.MessageTemplates
                    .FirstOrDefaultAsync(mt => mt.Id == templateId && mt.HostId == userId && mt.IsActive);

                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found or not active for user {UserId}", templateId, userId);
                    return false;
                }

                var processedSubject = await ProcessTemplateContentAsync(template.Subject, userId, bookingId, customData);
                var processedContent = await ProcessTemplateContentAsync(template.Content, userId, bookingId, customData);

                await _emailService.SendEmailAsync(toEmail, processedSubject, processedContent);
                
                _logger.LogInformation("Template email sent successfully. Template: {TemplateId}, To: {Email}", templateId, toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send template email. Template: {TemplateId}, To: {Email}", templateId, toEmail);
                return false;
            }
        }

        public async Task<bool> SendTemplateEmailAsync(string toEmail, MessageTemplateType templateType, string hostId, int? bookingId = null, Dictionary<string, string>? customData = null)
        {
            try
            {
                var template = await _context.MessageTemplates
                    .FirstOrDefaultAsync(mt => mt.Type == templateType && mt.HostId == hostId && mt.IsActive);

                if (template == null)
                {
                    _logger.LogWarning("No active template found for type {TemplateType} and host {HostId}", templateType, hostId);
                    return false;
                }

                return await SendTemplateEmailAsync(toEmail, template.Id, hostId, bookingId, customData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send template email by type. Type: {TemplateType}, Host: {HostId}", templateType, hostId);
                return false;
            }
        }

        public async Task<List<MessageTemplate>> GetActiveTemplatesAsync(string hostId, MessageTemplateType? type = null)
        {
            var query = _context.MessageTemplates
                .Where(mt => mt.HostId == hostId && mt.IsActive);

            if (type.HasValue)
            {
                query = query.Where(mt => mt.Type == type.Value);
            }

            return await query
                .OrderBy(mt => mt.Type)
                .ThenBy(mt => mt.Name)
                .ToListAsync();
        }

        public async Task<MessageTemplate?> GetTemplateAsync(int templateId, string hostId)
        {
            return await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == templateId && mt.HostId == hostId);
        }

        private async Task<string> ProcessTemplateContentAsync(MessageTemplate template, string userId, int? bookingId = null, Dictionary<string, string>? customData = null)
        {
            return await ProcessTemplateContentAsync(template.Content, userId, bookingId, customData);
        }

        private async Task<string> ProcessTemplateContentAsync(string content, string userId, int? bookingId = null, Dictionary<string, string>? customData = null)
        {
            var processedContent = content;

            // Get user/host information
            var host = await _userManager.FindByIdAsync(userId);
            if (host != null)
            {
                processedContent = processedContent
                    .Replace("{hostName}", $"{host.FirstName} {host.LastName}".Trim())
                    .Replace("{hostPhone}", host.PhoneNumber ?? "")
                    .Replace("{hostEmail}", host.Email ?? "");
            }

            // Get booking information if provided
            if (bookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .FirstOrDefaultAsync(b => b.Id == bookingId.Value);

                if (booking != null)
                {
                    processedContent = processedContent
                        .Replace("{bookingId}", booking.Id.ToString())
                        .Replace("{guestName}", $"{booking.User.FirstName} {booking.User.LastName}".Trim())
                        .Replace("{guestEmail}", booking.User.Email ?? "")
                        .Replace("{homestayName}", booking.Homestay.Name)
                        .Replace("{address}", booking.Homestay.Address)
                        .Replace("{checkInDate}", booking.CheckInDate.ToString("dd/MM/yyyy"))
                        .Replace("{checkOutDate}", booking.CheckOutDate.ToString("dd/MM/yyyy"))
                        .Replace("{checkInTime}", "14:00") // Default check-in time
                        .Replace("{checkOutTime}", "12:00") // Default check-out time
                        .Replace("{totalAmount}", booking.TotalAmount.ToString("N0") + " VNĐ")
                        .Replace("{nights}", (booking.CheckOutDate - booking.CheckInDate).Days.ToString());
                }
            }

            // Apply custom data
            if (customData != null)
            {
                foreach (var kvp in customData)
                {
                    processedContent = processedContent.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            // Default placeholders if not replaced
            processedContent = processedContent
                .Replace("{wifiName}", "WiFi_Homestay")
                .Replace("{wifiPassword}", "******")
                .Replace("{cancellationReason}", "Lý do hủy không xác định");

            return processedContent;
        }
    }
}
