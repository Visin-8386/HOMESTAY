using Microsoft.AspNetCore.Mvc;
using WebHS.Services.Enhanced;
using WebHS.Services;

namespace WebHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeocodingDebugController : Controller
    {
        private readonly GoogleGeocodingService _googleService;
        private readonly EnhancedGeocodingService _nominatimService;
        private readonly HybridGeocodingService _hybridService;
        private readonly ILogger<GeocodingDebugController> _logger;

        public GeocodingDebugController(
            GoogleGeocodingService googleService,
            EnhancedGeocodingService nominatimService,
            HybridGeocodingService hybridService,
            ILogger<GeocodingDebugController> logger)
        {
            _googleService = googleService;
            _nominatimService = nominatimService;
            _hybridService = hybridService;
            _logger = logger;
        }

        [HttpGet("compare")]
        public async Task<IActionResult> CompareGeocoding([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address is required");
            }

            try
            {
                // Test Google Maps trực tiếp
                var googleResult = await _googleService.GetCoordinatesAsync(address);
                var googleData = new
                {
                    success = googleResult.latitude.HasValue && googleResult.longitude.HasValue,
                    latitude = googleResult.latitude,
                    longitude = googleResult.longitude,
                    source = "Google Maps Geocoding API",
                    googleMapsUrl = googleResult.latitude.HasValue && googleResult.longitude.HasValue 
                        ? $"https://www.google.com/maps/@{googleResult.latitude},{googleResult.longitude},15z"
                        : (string?)null
                };

                // Test Nominatim
                var nominatimResult = await _nominatimService.GetCoordinatesAsync(address);
                var nominatimData = new
                {
                    success = nominatimResult.latitude.HasValue && nominatimResult.longitude.HasValue,
                    latitude = nominatimResult.latitude,
                    longitude = nominatimResult.longitude,
                    source = "Nominatim (OpenStreetMap)",
                    googleMapsUrl = nominatimResult.latitude.HasValue && nominatimResult.longitude.HasValue 
                        ? $"https://www.google.com/maps/@{nominatimResult.latitude},{nominatimResult.longitude},15z"
                        : (string?)null
                };

                // Test Hybrid
                var hybridResult = await _hybridService.GetCoordinatesAsync(address);
                var hybridData = new
                {
                    success = hybridResult.latitude.HasValue && hybridResult.longitude.HasValue,
                    latitude = hybridResult.latitude,
                    longitude = hybridResult.longitude,
                    source = "Hybrid Service (Auto-fallback)",
                    googleMapsUrl = hybridResult.latitude.HasValue && hybridResult.longitude.HasValue 
                        ? $"https://www.google.com/maps/@{hybridResult.latitude},{hybridResult.longitude},15z"
                        : (string?)null
                };

                var results = new
                {
                    originalAddress = address,
                    googleMaps = googleData,
                    nominatim = nominatimData,
                    hybrid = hybridData,
                    timestamp = DateTime.Now
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in geocoding comparison for: {Address}", address);
                return Ok(new
                {
                    originalAddress = address,
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("test-formats")]
        public async Task<IActionResult> TestAddressFormats([FromQuery] string baseAddress)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                return BadRequest("Base address is required");
            }

            var formats = new[]
            {
                baseAddress,
                $"{baseAddress}, Vietnam",
                $"{baseAddress}, Việt Nam", 
                $"{baseAddress}, Ho Chi Minh City, Vietnam",
                $"{baseAddress}, TP.HCM, Việt Nam",
                $"{baseAddress}, Hà Nội, Việt Nam"
            };

            var results = new List<object>();

            foreach (var format in formats)
            {
                try
                {
                    var result = await _googleService.GetCoordinatesAsync(format);
                    results.Add(new
                    {
                        format = format,
                        success = result.latitude.HasValue && result.longitude.HasValue,
                        latitude = result.latitude,
                        longitude = result.longitude,
                        googleMapsUrl = result.latitude.HasValue && result.longitude.HasValue 
                            ? $"https://www.google.com/maps/@{result.latitude},{result.longitude},15z"
                            : null
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        format = format,
                        success = false,
                        error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                baseAddress = baseAddress,
                testedFormats = results,
                timestamp = DateTime.Now
            });
        }

        [HttpGet("test-google-direct")]
        public async Task<IActionResult> TestGoogleDirect([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address is required");
            }

            try
            {
                // Test với nhiều format khác nhau như Google Maps web
                var testFormats = new[]
                {
                    address, // Địa chỉ gốc
                    $"{address}, Vietnam", // Thêm Vietnam
                    $"{address}, Việt Nam", // Thêm Việt Nam
                    Uri.EscapeDataString(address), // URL encoded
                    Uri.EscapeDataString($"{address}, Vietnam") // URL encoded + Vietnam
                };

                var results = new List<object>();
                var apiKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ExternalAPIs:GoogleMaps:ApiKey"];

                foreach (var format in testFormats)
                {
                    try
                    {
                        var httpClient = HttpContext.RequestServices.GetRequiredService<HttpClient>();
                        
                        // Tạo URL giống hệt như trong GoogleGeocodingService
                        var encodedAddress = Uri.EscapeDataString(format);
                        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&region=vn&language=vi&key={apiKey}";

                        var response = await httpClient.GetAsync(url);
                        var jsonContent = await response.Content.ReadAsStringAsync();

                        results.Add(new
                        {
                            format = format,
                            url = url.Replace(apiKey ?? "", "***API_KEY***"), // Ẩn API key
                            success = response.IsSuccessStatusCode,
                            statusCode = (int)response.StatusCode,
                            rawResponse = jsonContent,
                            timestamp = DateTime.Now
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            format = format,
                            success = false,
                            error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    originalAddress = address,
                    apiKey = string.IsNullOrEmpty(apiKey) ? "NOT_CONFIGURED" : "CONFIGURED",
                    testResults = results,
                    instructions = new
                    {
                        message = "So sánh kết quả với Google Maps bằng cách:",
                        steps = new[]
                        {
                            "1. Mở Google Maps (maps.google.com)",
                            "2. Search địa chỉ giống hệt như 'originalAddress'",
                            "3. Nhấn chuột phải vào pin đỏ trên map",
                            "4. Chọn 'What's here?' hoặc copy tọa độ",
                            "5. So sánh lat, lng với kết quả ở đây"
                        }
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct Google test for: {Address}", address);
                return Ok(new
                {
                    originalAddress = address,
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("parse-google-response")]
        public IActionResult ParseGoogleResponse([FromQuery] string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return BadRequest("JSON response is required");
            }

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                var status = root.GetProperty("status").GetString();
                
                if (status == "OK" && root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    var geometry = firstResult.GetProperty("geometry");
                    var location = geometry.GetProperty("location");
                    
                    var lat = location.GetProperty("lat").GetDouble();
                    var lng = location.GetProperty("lng").GetDouble();
                    var formattedAddress = firstResult.GetProperty("formatted_address").GetString();

                    return Ok(new
                    {
                        success = true,
                        status = status,
                        latitude = lat,
                        longitude = lng,
                        formattedAddress = formattedAddress,
                        googleMapsUrl = $"https://www.google.com/maps/@{lat},{lng},15z",
                        googleMapsSearchUrl = $"https://www.google.com/maps/search/{Uri.EscapeDataString(formattedAddress ?? "")}",
                        coordinates = $"{lat}, {lng}",
                        instructions = "Copy tọa độ này và paste vào Google Maps search để kiểm tra"
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        status = status,
                        message = "No results found or API error"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Invalid JSON format"
                });
            }
        }

        [HttpGet("test-page")]
        public IActionResult TestPage()
        {
            return View("~/Views/Shared/GeocodingTest.cshtml");
        }

        [HttpGet("debug-google")]
        public async Task<IActionResult> DebugGoogle([FromQuery] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest("Address is required");
            }

            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = config["ExternalAPIs:GoogleMaps:ApiKey"];
            
            try
            {
                var httpClient = HttpContext.RequestServices.GetRequiredService<HttpClient>();
                
                // Test với địa chỉ đơn giản trước
                var encodedAddress = Uri.EscapeDataString($"{address}, Vietnam");
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&region=vn&language=vi&key={apiKey}";

                _logger.LogInformation("Testing Google Geocoding API with URL: {Url}", url.Replace(apiKey ?? "", "***"));

                var response = await httpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                // Parse response để xem lỗi gì
                using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
                var root = document.RootElement;
                var status = root.GetProperty("status").GetString();

                var debugInfo = new
                {
                    request = new
                    {
                        originalAddress = address,
                        encodedAddress = encodedAddress,
                        url = url.Replace(apiKey ?? "", "***API_KEY***"),
                        apiKeyConfigured = !string.IsNullOrEmpty(apiKey),
                        apiKeyLength = apiKey?.Length ?? 0
                    },
                    response = new
                    {
                        statusCode = (int)response.StatusCode,
                        isSuccess = response.IsSuccessStatusCode,
                        googleStatus = status,
                        rawResponse = jsonContent
                    },
                    analysis = new
                    {
                        message = GetGoogleErrorMessage(status),
                        suggestions = GetGoogleErrorSuggestions(status, apiKey)
                    },
                    timestamp = DateTime.Now
                };

                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging Google Geocoding");
                return Ok(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    apiKeyConfigured = !string.IsNullOrEmpty(apiKey),
                    timestamp = DateTime.Now
                });
            }
        }

        private string GetGoogleErrorMessage(string? status)
        {
            return status switch
            {
                "OK" => "✅ Thành công",
                "ZERO_RESULTS" => "❌ Không tìm thấy kết quả cho địa chỉ này",
                "OVER_QUERY_LIMIT" => "⚠️ Đã vượt quá giới hạn truy vấn API",
                "REQUEST_DENIED" => "🚫 Yêu cầu bị từ chối - Kiểm tra API Key",
                "INVALID_REQUEST" => "❓ Yêu cầu không hợp lệ - Kiểm tra tham số",
                "UNKNOWN_ERROR" => "🔄 Lỗi server - Thử lại sau",
                _ => $"❓ Status không xác định: {status}"
            };
        }

        private string[] GetGoogleErrorSuggestions(string? status, string? apiKey)
        {
            return status switch
            {
                "REQUEST_DENIED" => new[]
                {
                    "1. Kiểm tra API Key có đúng không",
                    "2. Vào Google Cloud Console → APIs & Services → Credentials",
                    "3. Đảm bảo Geocoding API đã được enable",
                    "4. Kiểm tra billing account đã được setup",
                    "5. Kiểm tra API restrictions (nếu có)"
                },
                "OVER_QUERY_LIMIT" => new[]
                {
                    "1. Đã hết quota miễn phí ($200/tháng)",
                    "2. Cần thêm payment method",
                    "3. Hoặc đợi tháng sau để reset quota"
                },
                "ZERO_RESULTS" => new[]
                {
                    "1. Thử địa chỉ đơn giản hơn",
                    "2. Thêm thành phố/quốc gia",
                    "3. Kiểm tra chính tả địa chỉ"
                },
                _ when string.IsNullOrEmpty(apiKey) => new[]
                {
                    "1. API Key chưa được cấu hình",
                    "2. Kiểm tra appsettings.json → ExternalAPIs:GoogleMaps:ApiKey"
                },
                _ => new[]
                {
                    "1. Thử lại với địa chỉ khác",
                    "2. Kiểm tra kết nối internet",
                    "3. Xem logs để biết thêm chi tiết"
                }
            };
        }
    }
}
