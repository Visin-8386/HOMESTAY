using WebHS.Services;

namespace WebHS.Services
{
    public class MockGeminiService : IGeminiService
    {
        private readonly ILogger<MockGeminiService> _logger;

        public MockGeminiService(ILogger<MockGeminiService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            // Simulate API delay
            await Task.Delay(1000);
            
            // Simple mock responses for testing
            var responses = new[]
            {
                "Xin chào! Tôi là trợ lý AI của WebHS. Tôi có thể giúp bạn tìm homestay phù hợp với nhu cầu của bạn.",
                "Về homestay, chúng tôi có nhiều lựa chọn tuyệt vời ở các địa điểm du lịch nổi tiếng như Đà Lạt, Sapa, Hội An...",
                "Để đặt phòng homestay, bạn có thể xem danh sách, chọn phòng phù hợp và thực hiện thanh toán online.",
                "Giá homestay dao động từ 300,000 - 2,000,000 VNĐ/đêm tùy theo vị trí và tiện ích.",
                "Bạn có thể xem video, hình ảnh và đánh giá của khách hàng trước khi quyết định đặt phòng."
            };
            
            var random = new Random();
            var response = responses[random.Next(responses.Length)];
            
            _logger.LogInformation("Mock AI response generated for prompt: {Prompt}", prompt);
            
            return response;
        }

        public async Task<string> ChatAsync(string message, List<ChatMessage>? history = null)
        {
            return await GenerateResponseAsync(message);
        }
    }
}
