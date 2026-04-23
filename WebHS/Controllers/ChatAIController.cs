using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatAIController : ControllerBase
    {
        private readonly ILogger<ChatAIController> _logger;

        public ChatAIController(ILogger<ChatAIController> logger)
        {
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest("Message cannot be empty");
                }

                // Simulate processing time
                await Task.Delay(500);

                var response = GenerateAIResponse(request.Message);
                
                return Ok(new ChatMessageResponse
                {
                    Message = response,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return StatusCode(500, "Internal server error");
            }
        }

        private string GenerateAIResponse(string userMessage)
        {
            var message = userMessage.ToLower();
            
            // Homestay search responses
            if (message.Contains("tìm") || message.Contains("homestay") || message.Contains("đặt phòng"))
            {
                return "Để tìm homestay phù hợp, bạn có thể:\n• Sử dụng thanh tìm kiếm trên trang chủ\n• Lọc theo vị trí, giá cả, tiện nghi\n• Xem danh sách homestay nổi bật\n• Đọc đánh giá từ khách hàng khác\n\nBạn muốn tìm homestay ở đâu ạ?";
            }
            
            // Booking process responses
            if (message.Contains("đặt") || message.Contains("booking") || message.Contains("thanh toán"))
            {
                return "Quy trình đặt phòng gồm:\n1. Chọn homestay và ngày ở\n2. Điền thông tin khách hàng\n3. Xác nhận đặt phòng\n4. Thanh toán online\n5. Nhận xác nhận qua email\n\nBạn cần hỗ trợ bước nào ạ?";
            }
            
            // Account/login responses
            if (message.Contains("đăng nhập") || message.Contains("tài khoản") || message.Contains("đăng ký"))
            {
                return "Về tài khoản:\n• Đăng ký tài khoản để đặt phòng dễ dàng\n• Theo dõi lịch sử đặt phòng\n• Nhận thông báo và ưu đãi\n• Trở thành Host để cho thuê\n\nBạn cần hỗ trợ gì về tài khoản ạ?";
            }
            
            // General pricing responses
            if (message.Contains("giá") || message.Contains("phí") || message.Contains("chi phí"))
            {
                return "Về giá cả:\n• Giá homestay tùy thuộc vào vị trí, tiện nghi\n• Có thể có phí dịch vụ và thuế\n• Áp dụng mã giảm giá nếu có\n• Thanh toán an toàn qua cổng thanh toán\n\nBạn muốn xem giá homestay nào ạ?";
            }
            
            // Support responses
            if (message.Contains("hỗ trợ") || message.Contains("giúp") || message.Contains("liên hệ"))
            {
                return "Các cách liên hệ hỗ trợ:\n• Chat trực tiếp với tôi\n• Email: support@homestay.com\n• Hotline: 1900-xxxx\n• Fanpage Facebook\n\nTôi có thể giúp gì khác cho bạn?";
            }
            
            // Greetings
            if (message.Contains("xin chào") || message.Contains("hello") || message.Contains("hi"))
            {
                return "Xin chào! Rất vui được hỗ trợ bạn. Tôi có thể giúp bạn tìm homestay, giải đáp thắc mắc về đặt phòng hoặc hướng dẫn sử dụng website. Bạn cần hỗ trợ gì ạ?";
            }
            
            // Thanks
            if (message.Contains("cảm ơn") || message.Contains("thanks") || message.Contains("thank you"))
            {
                return "Rất vui được giúp đỡ bạn! Nếu có thêm câu hỏi nào khác, đừng ngại hỏi tôi nhé. Chúc bạn có trải nghiệm tuyệt vời với Homestay Booking! 😊";
            }
            
            // Default responses
            var responses = new[]
            {
                "Tôi hiểu bạn đang hỏi về điều này. Bạn có thể cung cấp thêm chi tiết để tôi hỗ trợ tốt hơn không ạ?",
                "Đây là câu hỏi thú vị! Tôi sẽ cố gắng giúp bạn. Bạn có thể nói rõ hơn về vấn đề này không?",
                "Tôi có thể giúp bạn về việc tìm homestay, đặt phòng, hoặc sử dụng website. Bạn cần hỗ trợ vấn đề gì cụ thể ạ?",
                "Để tôi hỗ trợ bạn tốt nhất, bạn có thể nói rõ hơn về câu hỏi của mình không ạ?"
            };
            
            var random = new Random();
            return responses[random.Next(responses.Length)];
        }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatMessageResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
