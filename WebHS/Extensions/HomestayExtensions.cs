using WebHS.Services;

namespace WebHS.Extensions
{
    public static class HomestayExtensions
    {
        public static async Task<(decimal latitude, decimal longitude)> GetCoordinatesFromAddressAsync(
            this GeocodingService geocodingService, 
            string address, 
            string? city = null, 
            string? district = null, 
            string? ward = null,
            string country = "Vietnam")
        {
            // Tạo địa chỉ đầy đủ cho việc geocoding
            var fullAddress = BuildFullAddress(address, ward, district, city, country);
            
            var (lat, lng) = await geocodingService.GetCoordinatesAsync(fullAddress);
            
            if (lat.HasValue && lng.HasValue)
            {
                return ((decimal)lat.Value, (decimal)lng.Value);
            }
            
            // Nếu không tìm thấy tọa độ, trả về tọa độ mặc định của Hà Nội
            return (21.0285m, 105.8542m);
        }

        private static string BuildFullAddress(string address, string? ward, string? district, string? city, string country)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(address))
                parts.Add(address.Trim());
                
            if (!string.IsNullOrWhiteSpace(ward))
                parts.Add(ward.Trim());
                
            if (!string.IsNullOrWhiteSpace(district))
                parts.Add(district.Trim());
                
            if (!string.IsNullOrWhiteSpace(city))
                parts.Add(city.Trim());
                
            if (!string.IsNullOrWhiteSpace(country))
                parts.Add(country.Trim());
            
            return string.Join(", ", parts);
        }

        public static async Task<string?> GetAddressFromCoordinatesAsync(
            this GeocodingService geocodingService,
            decimal latitude,
            decimal longitude)
        {
            return await geocodingService.GetAddressFromCoordinatesAsync((double)latitude, (double)longitude);
        }
    }
}
