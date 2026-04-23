namespace WebHS.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateResponseAsync(string prompt);
        Task<string> ChatAsync(string message, List<ChatMessage>? history = null);
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "model"
        public string Content { get; set; } = string.Empty;
    }
}
