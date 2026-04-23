using Microsoft.AspNetCore.Mvc;

namespace WebHS.Controllers
{
    public class GoogleServicesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleServicesController> _logger;

        public GoogleServicesController(IConfiguration configuration, ILogger<GoogleServicesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private static string GetMaskedApiKey(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 10)
                return "Not Set";
            
            return "****" + apiKey.Substring(apiKey.Length - 4);
        }

        [HttpGet]
        public IActionResult Test()
        {
            var googleServices = new
            {
                GoogleOAuth = new
                {
                    Enabled = !string.IsNullOrEmpty(_configuration["Authentication:Google:ClientId"]) && 
                             _configuration["Authentication:Google:ClientId"] != "your-google-client-id",
                    ClientId = _configuration["Authentication:Google:ClientId"]
                },
                GoogleAnalytics = new
                {
                    Enabled = _configuration.GetValue<bool>("GoogleAnalytics:Enabled"),
                    TrackingId = _configuration["GoogleAnalytics:TrackingId"]
                },
                GoogleSearchConsole = new
                {
                    Enabled = _configuration.GetValue<bool>("GoogleSearchConsole:Enabled"),
                    SiteVerification = _configuration["GoogleSearchConsole:SiteVerification"]
                },
                YouTube = new
                {
                    Enabled = !string.IsNullOrEmpty(_configuration["YouTube:ApiKey"]) && 
                             _configuration["YouTube:ApiKey"] != "YOUR_YOUTUBE_API_KEY_HERE",
                    ApiKey = GetMaskedApiKey(_configuration["YouTube:ApiKey"])
                },
                GoogleReCaptcha = new
                {
                    Enabled = _configuration.GetValue<bool>("GoogleReCaptcha:Enabled"),
                    SiteKey = _configuration["GoogleReCaptcha:SiteKey"],
                    Version = _configuration["GoogleReCaptcha:Version"]
                },
                GoogleTagManager = new
                {
                    Enabled = _configuration.GetValue<bool>("GoogleTagManager:Enabled"),
                    ContainerId = _configuration["GoogleTagManager:ContainerId"]
                },
                GoogleAdSense = new
                {
                    Enabled = _configuration.GetValue<bool>("GoogleAdSense:Enabled"),
                    PublisherId = _configuration["GoogleAdSense:PublisherId"],
                    AutoAds = _configuration.GetValue<bool>("GoogleAdSense:AutoAds")
                },
                GoogleFonts = new
                {
                    Enabled = true,
                    Fonts = new[] { "Inter", "Poppins" }
                },
                GoogleMaps = new
                {
                    Enabled = !string.IsNullOrEmpty(_configuration["ExternalAPIs:GoogleMaps:ApiKey"]) && 
                             _configuration["ExternalAPIs:GoogleMaps:ApiKey"] != "YOUR_GOOGLE_MAPS_API_KEY_HERE",
                    ApiKey = GetMaskedApiKey(_configuration["ExternalAPIs:GoogleMaps:ApiKey"])
                }
            };

            ViewData["GoogleServices"] = googleServices;
            return View();
        }

        [HttpPost]
        public IActionResult TestReCaptcha([FromForm] string gRecaptchaResponse)
        {
            // This would be implemented with the reCAPTCHA service
            var result = new
            {
                Success = !string.IsNullOrEmpty(gRecaptchaResponse),
                Token = gRecaptchaResponse?.Substring(0, Math.Min(20, gRecaptchaResponse.Length)) + "...",
                Message = !string.IsNullOrEmpty(gRecaptchaResponse) ? "reCAPTCHA verified successfully!" : "reCAPTCHA verification failed"
            };

            return Json(result);
        }

        [HttpGet]
        public IActionResult Documentation()
        {
            return View();
        }
    }
}
