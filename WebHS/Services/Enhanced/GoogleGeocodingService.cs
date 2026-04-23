using System.Text.Json;

namespace WebHS.Services.Enhanced
{
    public class GoogleGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleGeocodingService> _logger;
        private readonly string _apiKey;

        public GoogleGeocodingService(HttpClient httpClient, ILogger<GoogleGeocodingService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["GoogleMaps:ApiKey"] ?? "";
        }

        public async Task<(double? latitude, double? longitude)> GetCoordinatesAsync(string address)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Maps API key not configured");
                return (null, null);
            }

            try
            {
                // Google Geocoding API với focus cho Việt Nam
                var encodedAddress = Uri.EscapeDataString($"{address}, Vietnam");
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&region=vn&language=vi&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleGeocodingResponse>(jsonContent);

                    if (result?.status == "OK" && result.results?.Length > 0)
                    {
                        var location = result.results[0].geometry.location;
                        return (location.lat, location.lng);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error geocoding with Google Maps: {address}");
            }

            return (null, null);
        }

        public async Task<GoogleAddressResponse?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Maps API key not configured");
                return null;
            }

            try
            {
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&region=vn&language=vi&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleGeocodingResponse>(jsonContent);

                    if (result?.status == "OK" && result.results?.Length > 0)
                    {
                        return ParseGoogleResponse(result.results[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reverse geocoding with Google Maps: {latitude}, {longitude}");
            }

            return null;
        }

        private GoogleAddressResponse ParseGoogleResponse(GoogleResult googleResult)
        {
            var response = new GoogleAddressResponse
            {
                FormattedAddress = googleResult.formatted_address,
                Success = true,
                Source = "Google Maps"
            };

            // Parse địa chỉ Việt Nam từ Google response
            foreach (var component in googleResult.address_components)
            {
                var types = component.types;
                
                if (types.Contains("street_number"))
                    response.HouseNumber = component.long_name;
                
                if (types.Contains("route"))
                    response.StreetName = component.long_name;
                
                if (types.Contains("sublocality_level_1") || types.Contains("ward"))
                    response.Ward = component.long_name;
                
                if (types.Contains("administrative_area_level_2") || types.Contains("locality"))
                    response.District = component.long_name;
                
                if (types.Contains("administrative_area_level_1"))
                    response.Province = component.long_name;
                
                if (types.Contains("country"))
                    response.Country = component.long_name;
                
                if (types.Contains("postal_code"))
                    response.PostCode = component.long_name;
            }

            return response;
        }
    }

    // Google Maps API Response Models
    public class GoogleGeocodingResponse
    {
        public string status { get; set; } = "";
        public GoogleResult[]? results { get; set; }
    }

    public class GoogleResult
    {
        public string formatted_address { get; set; } = "";
        public GoogleGeometry geometry { get; set; } = new();
        public GoogleAddressComponent[] address_components { get; set; } = Array.Empty<GoogleAddressComponent>();
    }

    public class GoogleGeometry
    {
        public GoogleLocation location { get; set; } = new();
    }

    public class GoogleLocation
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class GoogleAddressComponent
    {
        public string long_name { get; set; } = "";
        public string short_name { get; set; } = "";
        public string[] types { get; set; } = Array.Empty<string>();
    }

    public class GoogleAddressResponse
    {
        public bool Success { get; set; }
        public string FormattedAddress { get; set; } = "";
        public string HouseNumber { get; set; } = "";
        public string StreetName { get; set; } = "";
        public string Ward { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string Country { get; set; } = "";
        public string PostCode { get; set; } = "";
        public string Source { get; set; } = "";
    }
}
