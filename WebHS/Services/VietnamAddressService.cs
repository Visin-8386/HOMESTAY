using System.Text.Json;

namespace WebHS.Services
{
    public class VietnamAddressService
    {
        private readonly ILogger<VietnamAddressService> _logger;
        private readonly List<VietnamProvince> _provinces;
        private readonly Dictionary<string, (double Lat, double Lng)> _cityCoordinates;

        public VietnamAddressService(ILogger<VietnamAddressService> logger)
        {
            _logger = logger;
            _provinces = LoadVietnamProvinces();
            _cityCoordinates = LoadCityCoordinates();
        }

        public Task<(double Latitude, double Longitude)?> GetApproximateCoordinatesAsync(string address)
        {
            // Parse address để tìm tỉnh/thành phố
            var province = FindProvinceFromAddress(address);
            if (province != null && _cityCoordinates.ContainsKey(province.Name))
            {
                var coords = _cityCoordinates[province.Name];
                _logger.LogInformation("Found coordinates for {Province}: {Lat}, {Lng}", province.Name, coords.Lat, coords.Lng);
                return Task.FromResult<(double Latitude, double Longitude)?>(coords);
            }

            // Fallback: tìm theo từ khóa
            foreach (var kvp in _cityCoordinates)
            {
                if (address.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found approximate coordinates for {City}: {Lat}, {Lng}", kvp.Key, kvp.Value.Lat, kvp.Value.Lng);
                    return Task.FromResult<(double Latitude, double Longitude)?>(kvp.Value);
                }
            }

            return Task.FromResult<(double Latitude, double Longitude)?>(null);
        }

        public Task<VietnamLocalAddress?> GetNearestAddressAsync(double latitude, double longitude)
        {
            double minDistance = double.MaxValue;
            VietnamLocalAddress? nearestAddress = null;

            foreach (var kvp in _cityCoordinates)
            {
                var distance = CalculateDistance(latitude, longitude, kvp.Value.Lat, kvp.Value.Lng);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestAddress = new VietnamLocalAddress
                    {
                        Province = kvp.Key,
                        District = "Trung tâm",
                        Ward = "Trung tâm",
                        FullAddress = $"Trung tâm {kvp.Key}, Việt Nam",
                        Distance = distance
                    };
                }
            }

            if (nearestAddress != null)
            {
                _logger.LogInformation("Found nearest address: {Address} (Distance: {Distance:F2}km)", 
                    nearestAddress.FullAddress, nearestAddress.Distance);
            }

            return Task.FromResult(nearestAddress);
        }

        private VietnamProvince? FindProvinceFromAddress(string address)
        {
            return _provinces.FirstOrDefault(p => 
                address.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                address.Contains(p.Code, StringComparison.OrdinalIgnoreCase));
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // km
            var dLat = Math.PI * (lat2 - lat1) / 180;
            var dLon = Math.PI * (lon2 - lon1) / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(Math.PI * lat1 / 180) * Math.Cos(Math.PI * lat2 / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }

        private List<VietnamProvince> LoadVietnamProvinces()
        {
            return new List<VietnamProvince>
            {
                new() { Code = "01", Name = "Hà Nội" },
                new() { Code = "79", Name = "Thành phố Hồ Chí Minh" },
                new() { Code = "48", Name = "Đà Nẵng" },
                new() { Code = "92", Name = "Cần Thơ" },
                new() { Code = "31", Name = "Hải Phòng" },
                new() { Code = "26", Name = "Vĩnh Phúc" },
                new() { Code = "27", Name = "Bắc Ninh" },
                new() { Code = "30", Name = "Hải Dương" },
                new() { Code = "33", Name = "Hưng Yên" },
                new() { Code = "35", Name = "Hà Nam" },
                new() { Code = "36", Name = "Nam Định" },
                new() { Code = "37", Name = "Thái Bình" },
                new() { Code = "38", Name = "Ninh Bình" },
                new() { Code = "40", Name = "Thanh Hóa" },
                new() { Code = "42", Name = "Nghệ An" },
                new() { Code = "44", Name = "Hà Tĩnh" },
                new() { Code = "45", Name = "Quảng Bình" },
                new() { Code = "46", Name = "Quảng Trị" },
                new() { Code = "47", Name = "Thừa Thiên Huế" },
                new() { Code = "49", Name = "Quảng Nam" },
                new() { Code = "51", Name = "Quảng Ngãi" },
                new() { Code = "52", Name = "Bình Định" },
                new() { Code = "54", Name = "Phú Yên" },
                new() { Code = "56", Name = "Khánh Hòa" },
                new() { Code = "58", Name = "Ninh Thuận" },
                new() { Code = "60", Name = "Bình Thuận" },
                new() { Code = "62", Name = "Kon Tum" },
                new() { Code = "64", Name = "Gia Lai" },
                new() { Code = "66", Name = "Đắk Lắk" },
                new() { Code = "67", Name = "Đắk Nông" },
                new() { Code = "68", Name = "Lâm Đồng" },
                new() { Code = "70", Name = "Bình Phước" },
                new() { Code = "72", Name = "Tây Ninh" },
                new() { Code = "74", Name = "Bình Dương" },
                new() { Code = "75", Name = "Đồng Nai" },
                new() { Code = "77", Name = "Bà Rịa - Vũng Tàu" },
                new() { Code = "80", Name = "Long An" },
                new() { Code = "82", Name = "Tiền Giang" },
                new() { Code = "83", Name = "Bến Tre" },
                new() { Code = "84", Name = "Trà Vinh" },
                new() { Code = "86", Name = "Vĩnh Long" },
                new() { Code = "87", Name = "Đồng Tháp" },
                new() { Code = "89", Name = "An Giang" },
                new() { Code = "91", Name = "Kiên Giang" },
                new() { Code = "93", Name = "Hậu Giang" },
                new() { Code = "94", Name = "Sóc Trăng" },
                new() { Code = "95", Name = "Bạc Liêu" },
                new() { Code = "96", Name = "Cà Mau" }
            };
        }

        private Dictionary<string, (double Lat, double Lng)> LoadCityCoordinates()
        {
            return new Dictionary<string, (double Lat, double Lng)>(StringComparer.OrdinalIgnoreCase)
            {
                // Thành phố lớn
                { "Hà Nội", (21.0285, 105.8542) },
                { "Thành phố Hồ Chí Minh", (10.8231, 106.6297) },
                { "Hồ Chí Minh", (10.8231, 106.6297) },
                { "Sài Gòn", (10.8231, 106.6297) },
                { "Đà Nẵng", (16.0544, 108.2022) },
                { "Hải Phòng", (20.8449, 106.6881) },
                { "Cần Thơ", (10.0452, 105.7469) },

                // Các tỉnh khác
                { "Huế", (16.4674, 107.5905) },
                { "Thừa Thiên Huế", (16.4674, 107.5905) },
                { "Nha Trang", (12.2388, 109.1967) },
                { "Khánh Hòa", (12.2388, 109.1967) },
                { "Đà Lạt", (11.9404, 108.4583) },
                { "Lâm Đồng", (11.9404, 108.4583) },
                { "Hội An", (15.8801, 108.3380) },
                { "Quảng Nam", (15.8801, 108.3380) },
                { "Vũng Tàu", (10.4113, 107.1365) },
                { "Bà Rịa - Vũng Tàu", (10.4113, 107.1365) },
                { "Phú Quốc", (10.2899, 103.9840) },
                { "Kiên Giang", (10.2899, 103.9840) },
                { "Phan Thiết", (10.9333, 108.1000) },
                { "Bình Thuận", (10.9333, 108.1000) },
                { "Quy Nhon", (13.7563, 109.2297) },
                { "Bình Định", (13.7563, 109.2297) },
                { "Pleiku", (13.9833, 108.0000) },
                { "Gia Lai", (13.9833, 108.0000) },
                { "Buôn Ma Thuột", (12.6667, 108.0333) },
                { "Đắk Lắk", (12.6667, 108.0333) },
                
                // Miền Bắc
                { "Hạ Long", (20.9601, 107.0431) },
                { "Quảng Ninh", (20.9601, 107.0431) },
                { "Sapa", (22.3364, 103.8438) },
                { "Lào Cai", (22.3364, 103.8438) },
                { "Nghệ An", (18.6739, 105.6905) },
                { "Vinh", (18.6739, 105.6905) },
                { "Thanh Hóa", (19.8077, 105.7851) },
                { "Hà Tĩnh", (18.3559, 105.9069) },
                { "Quảng Bình", (17.4677, 106.6221) },
                { "Đồng Hới", (17.4677, 106.6221) },
                
                // Đồng bằng sông Cửu Long
                { "Mỹ Tho", (10.3600, 106.3597) },
                { "Tiền Giang", (10.3600, 106.3597) },
                { "Bến Tre", (10.2415, 106.3759) },
                { "Vĩnh Long", (10.2397, 105.9571) },
                { "Đồng Tháp", (10.4938, 105.6881) },
                { "Cao Lãnh", (10.4938, 105.6881) },
                { "An Giang", (10.3881, 105.4358) },
                { "Long Xuyên", (10.3881, 105.4358) },
                { "Châu Đốc", (10.7011, 105.1167) },
                { "Hậu Giang", (9.7781, 105.4681) },
                { "Vị Thanh", (9.7781, 105.4681) },
                { "Sóc Trăng", (9.6003, 105.9739) },
                { "Bạc Liêu", (9.2940, 105.7244) },
                { "Cà Mau", (9.1829, 105.1500) }
            };
        }
    }

    public class VietnamProvince
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class VietnamLocalAddress
    {
        public string Province { get; set; } = "";
        public string District { get; set; } = "";
        public string Ward { get; set; } = "";
        public string FullAddress { get; set; } = "";
        public double Distance { get; set; }
    }
}
