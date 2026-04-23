using System.Text.Json;

namespace WebHS.Services
{
    public interface IGoogleReCaptchaService
    {
        Task<bool> VerifyTokenAsync(string token, string? remoteIp = null);
        Task<ReCaptchaResponse> GetDetailedResponseAsync(string token, string? remoteIp = null);
    }

    public class GoogleReCaptchaService : IGoogleReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleReCaptchaService> _logger;
        private readonly string? _secretKey;
        private readonly bool _isEnabled;

        public GoogleReCaptchaService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<GoogleReCaptchaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _secretKey = _configuration["GoogleReCaptcha:SecretKey"];
            _isEnabled = _configuration.GetValue<bool>("GoogleReCaptcha:Enabled");
        }

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            if (!_isEnabled || string.IsNullOrEmpty(_secretKey) || _secretKey == "YOUR_RECAPTCHA_SECRET_KEY_HERE")
            {
                _logger.LogWarning("Google reCAPTCHA is not properly configured. Verification bypassed.");
                return true; // Allow through if not configured (development mode)
            }

            var response = await GetDetailedResponseAsync(token, remoteIp);
            return response.Success;
        }

        public async Task<ReCaptchaResponse> GetDetailedResponseAsync(string token, string? remoteIp = null)
        {
            if (!_isEnabled || string.IsNullOrEmpty(_secretKey) || _secretKey == "YOUR_RECAPTCHA_SECRET_KEY_HERE")
            {
                return new ReCaptchaResponse 
                { 
                    Success = true, 
                    Score = 1.0,
                    Action = "bypass",
                    Hostname = "localhost"
                };
            }

            try
            {
                var parameters = new List<KeyValuePair<string, string>>
                {
                    new("secret", _secretKey),
                    new("response", token)
                };

                if (!string.IsNullOrEmpty(remoteIp))
                {
                    parameters.Add(new("remoteip", remoteIp));
                }

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    _logger.LogInformation("reCAPTCHA verification result: Success={Success}, Score={Score}", 
                        result?.Success, result?.Score);

                    return result ?? new ReCaptchaResponse { Success = false };
                }
                else
                {
                    _logger.LogError("reCAPTCHA API request failed with status: {StatusCode}", response.StatusCode);
                    return new ReCaptchaResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA token");
                return new ReCaptchaResponse { Success = false };
            }
        }
    }

    public class ReCaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string? Hostname { get; set; }
        public string[]? ErrorCodes { get; set; }
    }
}
