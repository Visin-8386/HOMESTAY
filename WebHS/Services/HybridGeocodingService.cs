using WebHS.Services.Enhanced;

namespace WebHS.Services
{
    public class HybridGeocodingService
    {
        private readonly GoogleGeocodingService _googleService;
        private readonly EnhancedGeocodingService _nominatimService;
        private readonly VietnamAddressService _vietnamAddressService;
        private readonly ILogger<HybridGeocodingService> _logger;

        public HybridGeocodingService(
            GoogleGeocodingService googleService,
            EnhancedGeocodingService nominatimService,
            VietnamAddressService vietnamAddressService,
            ILogger<HybridGeocodingService> logger)
        {
            _googleService = googleService;
            _nominatimService = nominatimService;
            _vietnamAddressService = vietnamAddressService;
            _logger = logger;
        }

        public async Task<(double? latitude, double? longitude)> GetCoordinatesAsync(string address)
        {
            _logger.LogInformation("🔍 Starting hybrid geocoding for: {Address}", address);

            // 1. Thử Google Maps trước (độ chính xác cao nhất)
            try
            {
                var googleResult = await _googleService.GetCoordinatesAsync(address);
                if (googleResult.latitude.HasValue && googleResult.longitude.HasValue)
                {
                    _logger.LogInformation("✅ Google Maps geocoding successful: {Lat}, {Lng}", 
                        googleResult.latitude, googleResult.longitude);
                    return googleResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Google Maps geocoding failed, trying fallback");
            }

            // 2. Fallback: Nominatim (OpenStreetMap)
            try
            {
                var nominatimResult = await _nominatimService.GetCoordinatesAsync(address);
                if (nominatimResult.latitude.HasValue && nominatimResult.longitude.HasValue)
                {
                    _logger.LogInformation("✅ Nominatim geocoding successful: {Lat}, {Lng}", 
                        nominatimResult.latitude, nominatimResult.longitude);
                    return nominatimResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Nominatim geocoding failed, trying local database");
            }

            // 3. Fallback cuối: Vietnam Address Database
            try
            {
                var localResult = await _vietnamAddressService.GetApproximateCoordinatesAsync(address);
                if (localResult.HasValue)
                {
                    _logger.LogInformation("✅ Local database geocoding successful: {Lat}, {Lng}", 
                        localResult.Value.Latitude, localResult.Value.Longitude);
                    return (localResult.Value.Latitude, localResult.Value.Longitude);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Local database geocoding failed");
            }

            _logger.LogWarning("❌ All geocoding methods failed for address: {Address}", address);
            return (null, null);
        }

        public async Task<HybridAddressResponse?> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            _logger.LogInformation("🔍 Starting hybrid reverse geocoding for: {Lat}, {Lng}", latitude, longitude);

            // 1. Thử Google Maps trước
            try
            {
                var googleResult = await _googleService.GetAddressFromCoordinatesAsync(latitude, longitude);
                if (googleResult?.Success == true)
                {
                    _logger.LogInformation("✅ Google Maps reverse geocoding successful");
                    return new HybridAddressResponse(googleResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Google Maps reverse geocoding failed, trying fallback");
            }

            // 2. Fallback: Enhanced Nominatim
            try
            {
                var nominatimResult = await _nominatimService.GetEnhancedAddressFromCoordinatesAsync(latitude, longitude);
                if (nominatimResult?.Success == true)
                {
                    _logger.LogInformation("✅ Nominatim reverse geocoding successful");
                    return new HybridAddressResponse(nominatimResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Nominatim reverse geocoding failed, trying local lookup");
            }

            // 3. Fallback: Local Vietnam address lookup
            try
            {
                var localResult = await _vietnamAddressService.GetNearestAddressAsync(latitude, longitude);
                if (localResult != null)
                {
                    _logger.LogInformation("✅ Local address lookup successful");
                    return new HybridAddressResponse(localResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Local address lookup failed");
            }

            _logger.LogWarning("❌ All reverse geocoding methods failed for coordinates: {Lat}, {Lng}", latitude, longitude);
            return null;
        }
    }

    public class HybridAddressResponse
    {
        public bool Success { get; set; }
        public string FormattedAddress { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string HouseNumber { get; set; } = "";
        public string StreetName { get; set; } = "";
        public string Ward { get; set; } = "";
        public string District { get; set; } = "";
        public string Province { get; set; } = "";
        public string Country { get; set; } = "";
        public string PostCode { get; set; } = "";
        public string Source { get; set; } = "";
        public object? RawData { get; set; }

        // Constructor từ Google response
        public HybridAddressResponse(GoogleAddressResponse googleResponse)
        {
            Success = googleResponse.Success;
            FormattedAddress = googleResponse.FormattedAddress;
            DisplayName = googleResponse.FormattedAddress;
            HouseNumber = googleResponse.HouseNumber;
            StreetName = googleResponse.StreetName;
            Ward = googleResponse.Ward;
            District = googleResponse.District;
            Province = googleResponse.Province;
            Country = googleResponse.Country;
            PostCode = googleResponse.PostCode;
            Source = "Google Maps (Primary)";
        }

        // Constructor từ Nominatim response
        public HybridAddressResponse(EnhancedAddressResponse nominatimResponse)
        {
            Success = nominatimResponse.Success;
            FormattedAddress = nominatimResponse.FormattedAddress;
            DisplayName = nominatimResponse.DisplayName;
            HouseNumber = nominatimResponse.HouseNumber;
            StreetName = nominatimResponse.StreetName;
            Ward = nominatimResponse.Ward;
            District = nominatimResponse.District;
            Province = nominatimResponse.Province;
            Country = nominatimResponse.Country;
            PostCode = nominatimResponse.PostCode;
            Source = "Nominatim (Fallback)";
            RawData = nominatimResponse.RawData;
        }

        // Constructor từ local Vietnam database
        public HybridAddressResponse(VietnamLocalAddress localAddress)
        {
            Success = true;
            FormattedAddress = localAddress.FullAddress;
            DisplayName = localAddress.FullAddress;
            Ward = localAddress.Ward;
            District = localAddress.District;
            Province = localAddress.Province;
            Country = "Việt Nam";
            Source = "Vietnam Local Database (Fallback)";
        }
    }
}
