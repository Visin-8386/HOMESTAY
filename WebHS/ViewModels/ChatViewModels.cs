using System.Text.Json.Serialization;

namespace WebHS.ViewModels
{
    public class ChatViewModel
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public string NewMessage { get; set; } = string.Empty;
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int UserId { get; set; }
    }

    public class ChatApiRequest
    {
        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ChatApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
        
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }
}
