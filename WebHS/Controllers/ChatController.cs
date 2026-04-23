using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Models;
using WebHS.ViewModels;
using WebHS.Services;

namespace WebHS.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ChatController> _logger;
        private readonly IGeminiService _geminiService;

        public ChatController(
            UserManager<User> userManager,
            ILogger<ChatController> logger,
            IGeminiService geminiService)
        {
            _userManager = userManager;
            _logger = logger;
            _geminiService = geminiService;
        }

        public IActionResult Index()
        {
            var viewModel = new ChatViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous] // Tạm thời cho phép test không cần đăng nhập
        public async Task<IActionResult> SendMessage([FromBody] ChatApiRequest request)
        {
            try
            {
                _logger.LogInformation("Received chat message: {Message}", request?.Message);
                
                if (string.IsNullOrWhiteSpace(request?.Message))
                {
                    _logger.LogWarning("Empty message received");
                    return Json(new ChatApiResponse 
                    { 
                        Success = false, 
                        Error = "Tin nhắn không thể để trống" 
                    });
                }

                _logger.LogInformation("Calling GeminiService with message: {Message}", request.Message);
                
                // Use GeminiService to generate response
                var response = await _geminiService.GenerateResponseAsync(request.Message);
                
                _logger.LogInformation("GeminiService returned response: {Response}", response);
                
                return Json(new ChatApiResponse 
                { 
                    Success = true, 
                    Response = response 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat: {Message}", ex.Message);
                return Json(new ChatApiResponse 
                { 
                    Success = false, 
                    Error = "Có lỗi xảy ra khi xử lý tin nhắn. Vui lòng thử lại." 
                });
            }
        }

        [HttpGet]
        [AllowAnonymous] // Cho phép test không cần đăng nhập
        public async Task<IActionResult> TestGemini()
        {
            try
            {
                var testMessage = "Xin chào, bạn có thể giới thiệu về dịch vụ homestay không?";
                var response = await _geminiService.GenerateResponseAsync(testMessage);
                
                return Json(new { 
                    Success = true, 
                    TestMessage = testMessage,
                    Response = response,
                    Timestamp = DateTime.Now 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API test failed: {Message}", ex.Message);
                return Json(new { 
                    Success = false, 
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Timestamp = DateTime.Now 
                });
            }
        }
    }
}
