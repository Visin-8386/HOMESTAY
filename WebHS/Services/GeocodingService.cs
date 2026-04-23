using System.Text.Json;

namespace WebHS.Services
{
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeocodingService> _logger;

        public GeocodingService(HttpClient httpClient, ILogger<GeocodingService> logger)
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
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";

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

        public async Task<string?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                // Reverse geocoding
                var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "WebHS-Homestay/1.0");

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<NominatimReverseResult>(jsonContent);

                    return result?.display_name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reverse geocoding coordinates: {latitude}, {longitude}");
            }

            return null;
        }
    }

    public class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
    }

    public class NominatimReverseResult
    {
        public string display_name { get; set; } = string.Empty;
        public NominatimAddress address { get; set; } = new();
    }

    public class NominatimAddress
    {
        public string house_number { get; set; } = string.Empty;
        public string road { get; set; } = string.Empty;
        public string suburb { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string postcode { get; set; } = string.Empty;
    }
}
