using System.Text;
using System.Text.Json;

namespace WebHS.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<GeminiService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IWebsiteInfoService _websiteInfoService;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger, IWebsiteInfoService websiteInfoService)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ExternalAPIs:Gemini:ApiKey"] ?? configuration["Gemini:ApiKey"] ?? throw new ArgumentException("Gemini API key is required");
            _baseUrl = configuration["ExternalAPIs:Gemini:BaseUrl"] ?? configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            _logger = logger;
            _websiteInfoService = websiteInfoService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private async Task<string> GetSystemPromptAsync()
        {
            var stats = await _websiteInfoService.GetWebsiteStatsAsync();
            var homestayInfo = await _websiteInfoService.GetHomestayInfoAsync();
            var popularHomestays = await _websiteInfoService.GetPopularHomestaysAsync();
            
            return $@"Bạn là trợ lý AI thông minh của Đom Đóm Dream - nền tảng đặt phòng homestay hàng đầu Việt Nam.

THÔNG TIN VỀ ĐOM ĐÓM DREAM:
- Website: Đom Đóm Dream (Homestay Booking Platform)
- Chuyên về: Đặt phòng homestay, villa, biệt thự du lịch tại Việt Nam
- Dịch vụ chính: Tìm kiếm và đặt phòng homestay, quản lý đặt phòng, hỗ trợ chủ nhà
- Đối tượng: Du khách trong nước và quốc tế muốn trải nghiệm văn hóa địa phương

{stats}

{homestayInfo}

{popularHomestays}

TÍNH NĂNG CHÍNH:
1. Tìm kiếm homestay theo địa điểm, giá cả, tiện nghi
2. Đặt phòng trực tuyến với thanh toán an toàn (PayPal, chuyển khoản)
3. Đánh giá và nhận xét từ khách hàng thật
4. Hỗ trợ chủ nhà đăng ký và quản lý homestay
5. Chat trực tiếp với chủ nhà qua hệ thống tin nhắn
6. Thông tin thời tiết, địa điểm du lịch
7. Hệ thống thông báo và email tự động

CÁCH TRẢ LỜI:
- Sử dụng tiếng Việt thân thiện, chuyên nghiệp
- KHÔNG sử dụng ký hiệu ** hay # hoặc bất kỳ markdown formatting nào
- Trả lời bằng text thuần không có định dạng đặc biệt
- Dùng từ ngữ đơn giản, dễ hiểu
- Đưa ra gợi ý cụ thể dựa trên thông tin có sẵn
- KHI NHẮC ĐẾN HOMESTAY CỤ THỂ: Luôn cung cấp link trực tiếp từ thông tin có sẵn
- Khuyến khích khách click vào link để xem chi tiết và đặt phòng
- Khuyến khích khách hàng đặt phòng trên Đom Đóm Dream

HƯỚNG DẪN SỬ DỤNG:
- Trang chủ: Tìm kiếm homestay theo địa điểm
- Đăng ký/Đăng nhập: Tạo tài khoản để đặt phòng
- Đặt phòng: Chọn ngày, số khách, thanh toán
- Chat: Liên hệ trực tiếp với chủ nhà hoặc AI hỗ trợ
- Quản lý booking: Xem lịch sử đặt phòng, hủy phòng

HƯỚNG DẪN TRẢ LỜI:
- Luôn thân thiện, nhiệt tình và chuyên nghiệp
- Cung cấp thông tin chính xác về homestay và du lịch Việt Nam
- Hướng dẫn chi tiết cách sử dụng website khi được hỏi
- Gợi ý địa điểm du lịch phù hợp với nhu cầu khách hàng
- Giải đáp thắc mắc về quy trình đặt phòng, thanh toán, hủy phòng, chính sách
- Hỗ trợ cả khách hàng và chủ nhà
- Khi khách hàng hỏi về homestay cụ thể, hãy đề xuất từ danh sách trên với thông tin chi tiết
- Nếu không biết thông tin chi tiết, hãy gợi ý liên hệ bộ phận hỗ trợ

Hãy trả lời một cách tự nhiên, hữu ích và phù hợp với ngữ cảnh homestay/du lịch Việt Nam.";
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                var systemPrompt = await GetSystemPromptAsync();
                var fullPrompt = $"{systemPrompt}\n\nKhách hàng hỏi: {prompt}";
                
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024,
                        candidateCount = 1
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                _logger.LogInformation($"Sending request to Gemini API: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Gemini API response: Status={response.StatusCode}, Content={responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<GeminiResponse>(responseContent, _jsonOptions);
                        
                        _logger.LogInformation($"Parsed response - Candidates count: {result?.Candidates?.Length ?? 0}");
                        
                        if (result?.Candidates?.Any() == true)
                        {
                            var candidate = result.Candidates.First();
                            _logger.LogInformation($"First candidate - Content: {candidate.Content != null}, Parts count: {candidate.Content?.Parts?.Length ?? 0}");
                            
                            if (candidate.Content?.Parts?.Any() == true)
                            {
                                var text = candidate.Content.Parts.First().Text;
                                _logger.LogInformation($"Extracted text: {text}");
                                
                                if (!string.IsNullOrEmpty(text))
                                {
                                    return text;
                                }
                            }
                        }
                        
                        _logger.LogWarning("Gemini API returned empty or invalid response structure");
                        return "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này.";
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"Failed to parse JSON response: {responseContent}");
                        return "Xin lỗi, có lỗi xảy ra khi xử lý phản hồi từ AI.";
                    }
                }
                else
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseContent}");
                    return "Xin lỗi, có lỗi xảy ra khi xử lý yêu cầu của bạn.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "Xin lỗi, có lỗi xảy ra khi xử lý yêu cầu của bạn.";
            }
        }

        public async Task<string> ChatAsync(string message, List<ChatMessage>? history = null)
        {
            try
            {
                var contents = new List<object>();
                
                // Add system context as first message if no history
                if (history == null || !history.Any())
                {
                    var systemPrompt = await GetSystemPromptAsync();
                    contents.Add(new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    });
                }

                // Add conversation history
                if (history != null && history.Any())
                {
                    foreach (var msg in history)
                    {
                        contents.Add(new
                        {
                            parts = new[] { new { text = msg.Content } }
                        });
                    }
                }

                // Add current message
                contents.Add(new
                {
                    parts = new[] { new { text = message } }
                });

                var requestBody = new
                {
                    contents = contents.ToArray(),
                    generationConfig = new
                    {
                        temperature = 0.8,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024,
                        candidateCount = 1
                    },
                    safetySettings = new[]
                    {
                        new
                        {
                            category = "HARM_CATEGORY_HARASSMENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_HATE_SPEECH",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        },
                        new
                        {
                            category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                            threshold = "BLOCK_MEDIUM_AND_ABOVE"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                _logger.LogInformation($"Sending chat request to Gemini API: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Gemini API chat response: Status={response.StatusCode}, Content={responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<GeminiResponse>(responseContent, _jsonOptions);
                        
                        _logger.LogInformation($"Parsed chat response - Candidates count: {result?.Candidates?.Length ?? 0}");
                        
                        if (result?.Candidates?.Any() == true)
                        {
                            var candidate = result.Candidates.First();
                            _logger.LogInformation($"First candidate - Content: {candidate.Content != null}, Parts count: {candidate.Content?.Parts?.Length ?? 0}");
                            
                            if (candidate.Content?.Parts?.Any() == true)
                            {
                                var text = candidate.Content.Parts.First().Text;
                                _logger.LogInformation($"Extracted text: {text}");
                                
                                if (!string.IsNullOrEmpty(text))
                                {
                                    return text;
                                }
                            }
                        }
                        
                        _logger.LogWarning("Gemini API returned empty or invalid response structure");
                        return "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này.";
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"Failed to parse JSON response: {responseContent}");
                        return "Xin lỗi, có lỗi xảy ra khi xử lý phản hồi từ AI.";
                    }
                }
                else
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseContent}");
                    return "Xin lỗi, có lỗi xảy ra khi xử lý yêu cầu của bạn.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "Xin lỗi, có lỗi xảy ra khi xử lý yêu cầu của bạn.";
            }
        }
    }

    // Response models for Gemini API
    public class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
        public string? ResponseId { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
        public string? FinishReason { get; set; }
        public GeminiSafetyRating[]? SafetyRatings { get; set; }
        public double? AvgLogprobs { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
        public string? Role { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }

    public class GeminiSafetyRating
    {
        public string? Category { get; set; }
        public string? Probability { get; set; }
    }

    public class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }
}
