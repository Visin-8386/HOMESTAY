using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebHS.Services.Enhanced
{
    public class EnhancedGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EnhancedGeocodingService> _logger;

        public EnhancedGeocodingService(HttpClient httpClient, ILogger<EnhancedGeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(double? latitude, double? longitude)> GetCoordinatesAsync(string address)
        {
            try
            {
                // Sử dụng Nominatim API (OpenStreetMap) - miễn phí
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1&addressdetails=1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "WebHS-Homestay/1.0");

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<List<NominatimResult>>(jsonContent);

                    if (results != null && results.Count > 0)
                    {
                        var result = results[0];
                        if (double.TryParse(result.lat, out double lat) && 
                            double.TryParse(result.lon, out double lon))
                        {
                            return (lat, lon);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Geocoding API returned {response.StatusCode} for address: {address}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error geocoding address: {address}");
            }

            return (null, null);
        }

        public async Task<EnhancedAddressResponse?> GetEnhancedAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                // Reverse geocoding with addressdetails=1 for structured data
                var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "WebHS-Homestay/1.0");

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<NominatimReverseResult>(jsonContent);

                    if (result != null)
                    {
                        return ParseNominatimResponse(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reverse geocoding coordinates: {latitude}, {longitude}");
            }

            return null;
        }

        private EnhancedAddressResponse ParseNominatimResponse(NominatimReverseResult nominatimResult)
        {
            var response = new EnhancedAddressResponse
            {
                DisplayName = nominatimResult.display_name ?? "",
                Success = true,
                RawData = nominatimResult, // 🎯 STORE COMPLETE RAW DATA
                Source = "Nominatim"
            };

            // Extract structured address components
            if (nominatimResult.address != null)
            {
                var addr = nominatimResult.address;
                
                // 🎯 CUSTOM PARSING LOGIC ACCORDING TO USER REQUIREMENTS
                
                // Parse house number and street name from road field
                if (!string.IsNullOrEmpty(addr.road))
                {
                    ParseHouseNumberAndStreetFromRoad(addr.road, response);
                }
                
                // If no house number found in road, try house_number field
                if (string.IsNullOrEmpty(response.HouseNumber) && !string.IsNullOrEmpty(addr.house_number))
                {
                    response.HouseNumber = addr.house_number;
                }
                
                // Administrative levels according to user requirements
                response.District = addr.suburb ?? ""; // 🎯 suburb = Quận/Huyện
                response.Ward = addr.village ?? "";     // 🎯 village = Xã/Phường (optional)
                response.Province = addr.city ?? "";    // 🎯 city = Tỉnh/Thành phố
                response.Country = addr.country ?? "";
                response.PostCode = addr.postcode ?? "";
            }

            // If house number is empty, try to extract from display_name
            if (string.IsNullOrEmpty(response.HouseNumber) && !string.IsNullOrEmpty(response.DisplayName))
            {
                ExtractHouseNumberFromDisplayName(response);
            }

            // Create formatted address compatible with existing parsing logic
            response.FormattedAddress = CreateGoogleMapsLikeFormat(response);

            return response;
        }

        private string GetWard(NominatimAddress addr)
        {
            // Vietnamese administrative hierarchy mapping
            return addr.suburb ?? addr.village ?? addr.hamlet ?? "";
        }

        private string GetDistrict(NominatimAddress addr)
        {
            // Try different Nominatim fields that might contain district info
            return addr.city_district ?? addr.county ?? addr.municipality ?? "";
        }

        private string GetProvince(NominatimAddress addr)
        {
            return addr.state ?? addr.province ?? addr.city ?? "";
        }

        private void ExtractHouseNumberFromDisplayName(EnhancedAddressResponse response)
        {
            if (string.IsNullOrEmpty(response.DisplayName)) return;

            // Vietnamese address patterns
            var patterns = new[]
            {
                @"^(\d+[a-zA-Z]?(?:\/\d+[a-zA-Z]?)?),?\s*(.+)",  // "123A, Street Name" or "123/45, Street"
                @"^Số\s*(\d+[a-zA-Z]?(?:\/\d+[a-zA-Z]?)?),?\s*(.+)", // "Số 123A, Street"
                @"^([0-9\/A-Za-z\-]+),?\s*(.+)" // Flexible number pattern
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(response.DisplayName, pattern);
                if (match.Success && match.Groups.Count >= 3)
                {
                    var extractedNumber = match.Groups[1].Value.Trim();
                    var remainingAddress = match.Groups[2].Value.Trim();

                    // Only update if we got a valid house number
                    if (!string.IsNullOrEmpty(extractedNumber) && Regex.IsMatch(extractedNumber, @"\d"))
                    {
                        response.HouseNumber = extractedNumber;
                        
                        // Extract street name from remaining address
                        var streetParts = remainingAddress.Split(',');
                        if (streetParts.Length > 0)
                        {
                            var streetCandidate = streetParts[0].Trim();
                            if (!string.IsNullOrEmpty(streetCandidate))
                            {
                                response.StreetName = streetCandidate;
                            }
                        }
                    }
                    break;
                }
            }
        }

        // 🎯 CUSTOM METHOD: Parse house number and street name from road field
        private void ParseHouseNumberAndStreetFromRoad(string road, EnhancedAddressResponse response)
        {
            if (string.IsNullOrWhiteSpace(road))
                return;

            // Clean and normalize the road string
            var cleanRoad = road.Trim();
            
            // Pattern 1: Find "đường" keyword and split accordingly
            // Example: "123 Đường Nguyễn Huệ" -> House: "123", Street: "Đường Nguyễn Huệ"
            var duongMatch = Regex.Match(cleanRoad, @"^(.+?)\s+(đường\s+.+)$", RegexOptions.IgnoreCase);
            if (duongMatch.Success)
            {
                var beforeDuong = duongMatch.Groups[1].Value.Trim();
                var fromDuong = duongMatch.Groups[2].Value.Trim();
                
                // Extract house number from the part before "đường"
                var houseNumberMatch = Regex.Match(beforeDuong, @"^(\d+[a-zA-Z]?(?:\/\d+[a-zA-Z]?)?)");
                if (houseNumberMatch.Success)
                {
                    response.HouseNumber = houseNumberMatch.Groups[1].Value.Trim();
                    response.StreetName = fromDuong;
                    
                    Console.WriteLine($"🎯 Parsed from road (with đường): House='{response.HouseNumber}', Street='{response.StreetName}'");
                    return;
                }
            }
            
            // Pattern 2: No "đường" keyword, try standard parsing
            // Example: "123 Nguyễn Huệ" -> House: "123", Street: "Nguyễn Huệ"
            var standardMatch = Regex.Match(cleanRoad, @"^(\d+[a-zA-Z]?(?:\/\d+[a-zA-Z]?)?)\s+(.+)$");
            if (standardMatch.Success)
            {
                response.HouseNumber = standardMatch.Groups[1].Value.Trim();
                response.StreetName = standardMatch.Groups[2].Value.Trim();
                
                Console.WriteLine($"🎯 Parsed from road (standard): House='{response.HouseNumber}', Street='{response.StreetName}'");
                return;
            }
            
            // Pattern 3: Vietnamese format with "Số"
            // Example: "Số 123 Đường Nguyễn Huệ" -> House: "123", Street: "Đường Nguyễn Huệ"
            var soMatch = Regex.Match(cleanRoad, @"^(?:số\s*)?(\d+[a-zA-Z]?(?:\/\d+[a-zA-Z]?)?)\s+(đường\s+.+)$", RegexOptions.IgnoreCase);
            if (soMatch.Success)
            {
                response.HouseNumber = soMatch.Groups[1].Value.Trim();
                response.StreetName = soMatch.Groups[2].Value.Trim();
                
                Console.WriteLine($"🎯 Parsed from road (Số format): House='{response.HouseNumber}', Street='{response.StreetName}'");
                return;
            }
            
            // Pattern 4: Fallback - if no number found, use entire road as street name
            Console.WriteLine($"🎯 No house number found in road, using entire road as street: '{cleanRoad}'");
            response.StreetName = cleanRoad;
        }

        private string CreateGoogleMapsLikeFormat(EnhancedAddressResponse response)
        {
            var parts = new List<string>();

            // Add house number + street as first part
            if (!string.IsNullOrEmpty(response.HouseNumber) && !string.IsNullOrEmpty(response.StreetName))
            {
                parts.Add($"{response.HouseNumber} {response.StreetName}");
            }
            else if (!string.IsNullOrEmpty(response.StreetName))
            {
                parts.Add(response.StreetName);
            }

            // Add administrative levels
            if (!string.IsNullOrEmpty(response.Ward))
                parts.Add(response.Ward);
            
            if (!string.IsNullOrEmpty(response.District))
                parts.Add(response.District);
            
            if (!string.IsNullOrEmpty(response.Province))
                parts.Add(response.Province);

            return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }

    public class EnhancedAddressResponse
    {
        public bool Success { get; set; }
        public string DisplayName { get; set; } = "";
        public string FormattedAddress { get; set; } = "";
        public string HouseNumber { get; set; } = "";
        public string StreetName { get; set; } = "";
        public string Ward { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string Country { get; set; } = "";
        public string PostCode { get; set; } = "";
        public object? RawData { get; set; } // 🎯 RAW DATA FROM NOMINATIM
        public string Source { get; set; } = "Nominatim"; // 🎯 DATA SOURCE
    }

    // Enhanced Nominatim classes with more fields
    public class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
        public NominatimAddress? address { get; set; }
    }

    public class NominatimReverseResult
    {
        public string display_name { get; set; } = string.Empty;
        public NominatimAddress? address { get; set; }
    }

    public class NominatimAddress
    {
        public string house_number { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string suburb { get; set; } = string.Empty;
        public string village { get; set; } = string.Empty;
        public string hamlet { get; set; } = string.Empty;
        public string city_district { get; set; } = string.Empty;
        public string county { get; set; } = string.Empty;
        public string municipality { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string province { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string postcode { get; set; } = string.Empty;
    }
}
