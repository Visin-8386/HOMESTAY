using WebHS.Models;

namespace WebHS.Services
{
    public interface IMessageTemplateService
    {
        Task<string> ProcessTemplateAsync(int templateId, string userId, int? bookingId = null, Dictionary<string, string>? customData = null);
        Task<bool> SendTemplateEmailAsync(string toEmail, int templateId, string userId, int? bookingId = null, Dictionary<string, string>? customData = null);
        Task<bool> SendTemplateEmailAsync(string toEmail, MessageTemplateType templateType, string hostId, int? bookingId = null, Dictionary<string, string>? customData = null);
        Task<List<MessageTemplate>> GetActiveTemplatesAsync(string hostId, MessageTemplateType? type = null);
        Task<MessageTemplate?> GetTemplateAsync(int templateId, string hostId);
    }
}
