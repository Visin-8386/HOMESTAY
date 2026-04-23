using System.Text.Json;

namespace WebHS.Services
{
    public interface IWeatherService
    {
        Task<WeatherInfo> GetWeatherAsync(double latitude, double longitude);
        Task<WeatherInfo> GetWeatherAsync(string city);
        Task<WeatherForecast> GetForecastAsync(double latitude, double longitude, int days = 5);
        Task<WeatherForecast> GetForecastAsync(string city, int days = 5);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] ?? "";
            _baseUrl = configuration["ExternalAPIs:OpenWeatherMap:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
        }

        public async Task<WeatherInfo> GetWeatherAsync(double latitude, double longitude)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your-openweathermap-api-key")
            {
                _logger.LogWarning("OpenWeatherMap API key not configured - returning mock data");
                return GetMockWeatherData($"Lat: {latitude:F2}, Lng: {longitude:F2}");
            }

            try
            {
                var url = $"{_baseUrl}/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (weatherData != null)
                    {
                        return new WeatherInfo
                        {
                            Temperature = Math.Round(weatherData.Main.Temp),
                            Description = weatherData.Weather.FirstOrDefault()?.Description ?? "",
                            Humidity = weatherData.Main.Humidity,
                            WindSpeed = weatherData.Wind.Speed,
                            Icon = weatherData.Weather.FirstOrDefault()?.Icon ?? "",
                            City = weatherData.Name
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("Weather API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for coordinates: {Lat}, {Lon}", latitude, longitude);
            }

            // Return mock data as fallback instead of null
            _logger.LogInformation("Returning mock weather data as fallback for coordinates: {Lat}, {Lon}", latitude, longitude);
            return GetMockWeatherData($"Vị trí: {latitude:F2}, {longitude:F2}");
        }

        public async Task<WeatherInfo> GetWeatherAsync(string city)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your-openweathermap-api-key")
            {
                _logger.LogWarning("OpenWeatherMap API key not configured - returning mock data");
                return GetMockWeatherData(city);
            }

            try
            {
                var url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (weatherData != null)
                    {
                        return new WeatherInfo
                        {
                            Temperature = Math.Round(weatherData.Main.Temp),
                            Description = weatherData.Weather.FirstOrDefault()?.Description ?? "",
                            Humidity = weatherData.Main.Humidity,
                            WindSpeed = weatherData.Wind.Speed,
                            Icon = weatherData.Weather.FirstOrDefault()?.Icon ?? "",
                            City = weatherData.Name
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("Weather API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for city: {City}", city);
            }

            // Return mock data as fallback instead of null
            _logger.LogInformation("Returning mock weather data as fallback for city: {City}", city);
            return GetMockWeatherData(city);
        }

        private WeatherInfo GetMockWeatherData(string location)
        {
            // Generate semi-realistic mock data based on time and location
            var random = new Random(location.GetHashCode() + DateTime.Now.Hour);
            var temperature = 22 + random.Next(8); // 22-30°C range for Vietnam
            var humidity = 60 + random.Next(25); // 60-85% humidity
            var windSpeed = 1.0 + (random.NextDouble() * 3.0); // 1-4 m/s
            
            var descriptions = new[] {
                "Trời nắng đẹp",
                "Có mây nhẹ", 
                "Nắng ít mây",
                "Trời quang đãng",
                "Mây rải rác"
            };
            
            var icons = new[] { "01d", "02d", "03d", "04d" };
            
            return new WeatherInfo
            {
                Temperature = temperature,
                Description = descriptions[random.Next(descriptions.Length)],
                Humidity = humidity,
                WindSpeed = Math.Round(windSpeed, 1),
                Icon = icons[random.Next(icons.Length)],
                City = location
            };
        }

        public async Task<WeatherForecast> GetForecastAsync(double latitude, double longitude, int days = 5)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your-openweathermap-api-key")
            {
                _logger.LogWarning("OpenWeatherMap API key not configured - returning mock forecast data");
                return GetMockForecastData($"Lat: {latitude:F2}, Lng: {longitude:F2}", days);
            }

            try
            {
                var url = $"{_baseUrl}/forecast?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var forecastData = JsonSerializer.Deserialize<OpenWeatherMapForecastResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (forecastData != null && forecastData.List.Any())
                    {
                        return ProcessForecastData(forecastData, days);
                    }
                }
                else
                {
                    _logger.LogWarning("Weather forecast API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast for coordinates: {Lat}, {Lon}", latitude, longitude);
            }

            // Return mock data as fallback
            _logger.LogInformation("Returning mock forecast data as fallback for coordinates: {Lat}, {Lon}", latitude, longitude);
            return GetMockForecastData($"Lat: {latitude:F2}, Lng: {longitude:F2}", days);
        }

        public async Task<WeatherForecast> GetForecastAsync(string city, int days = 5)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "your-openweathermap-api-key")
            {
                _logger.LogWarning("OpenWeatherMap API key not configured - returning mock forecast data");
                return GetMockForecastData(city, days);
            }

            try
            {
                var url = $"{_baseUrl}/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric&lang=vi";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var forecastData = JsonSerializer.Deserialize<OpenWeatherMapForecastResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (forecastData != null && forecastData.List.Any())
                    {
                        return ProcessForecastData(forecastData, days);
                    }
                }
                else
                {
                    _logger.LogWarning("Weather forecast API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast for city: {City}", city);
            }

            // Return mock data as fallback
            _logger.LogInformation("Returning mock forecast data as fallback for city: {City}", city);
            return GetMockForecastData(city, days);
        }

        private WeatherForecast ProcessForecastData(OpenWeatherMapForecastResponse forecastData, int days)
        {
            var dailyForecasts = new List<DailyWeather>();
            var groupedByDay = forecastData.List
                .GroupBy(item => DateTime.UnixEpoch.AddSeconds(item.Dt).Date)
                .Take(days);

            foreach (var dayGroup in groupedByDay)
            {
                var dayItems = dayGroup.ToList();
                var minTemp = dayItems.Min(item => item.Main.Temp);
                var maxTemp = dayItems.Max(item => item.Main.Temp);
                var avgHumidity = (int)dayItems.Average(item => item.Main.Humidity);
                var avgWindSpeed = dayItems.Average(item => item.Wind.Speed);
                
                // Get the most common weather condition for the day
                var commonWeather = dayItems
                    .GroupBy(item => item.Weather.FirstOrDefault()?.Description ?? "")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "";
                
                var commonIcon = dayItems
                    .GroupBy(item => item.Weather.FirstOrDefault()?.Icon ?? "")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "01d";

                dailyForecasts.Add(new DailyWeather
                {
                    Date = dayGroup.Key,
                    MinTemperature = Math.Round(minTemp),
                    MaxTemperature = Math.Round(maxTemp),
                    Description = commonWeather,
                    Icon = commonIcon,
                    Humidity = avgHumidity,
                    WindSpeed = Math.Round(avgWindSpeed, 1)
                });
            }

            return new WeatherForecast
            {
                City = forecastData.City.Name,
                DailyForecasts = dailyForecasts
            };
        }

        private WeatherForecast GetMockForecastData(string location, int days)
        {
            var random = new Random(location.GetHashCode() + DateTime.Now.Day);
            var forecasts = new List<DailyWeather>();
            
            var descriptions = new[] {
                "Trời nắng đẹp",
                "Có mây nhẹ", 
                "Nắng ít mây",
                "Trời quang đãng",
                "Mây rải rác",
                "Có mưa nhẹ",
                "Mưa rào"
            };
            
            var icons = new[] { "01d", "02d", "03d", "04d", "09d", "10d" };

            for (int i = 0; i < days; i++)
            {
                var date = DateTime.Today.AddDays(i);
                var baseTemp = 26 + random.Next(-3, 6); // 23-32°C range
                var minTemp = baseTemp + random.Next(-2, 1);
                var maxTemp = baseTemp + random.Next(1, 5);
                
                forecasts.Add(new DailyWeather
                {
                    Date = date,
                    MinTemperature = minTemp,
                    MaxTemperature = maxTemp,
                    Description = descriptions[random.Next(descriptions.Length)],
                    Icon = icons[random.Next(icons.Length)],
                    Humidity = 60 + random.Next(25), // 60-85%
                    WindSpeed = Math.Round(1.0 + (random.NextDouble() * 3.0), 1) // 1-4 m/s
                });
            }

            return new WeatherForecast
            {
                City = location,
                DailyForecasts = forecasts
            };
        }
    }

    // Data Transfer Objects
    public class WeatherInfo
    {
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        
        public string IconUrl => $"https://openweathermap.org/img/wn/{Icon}@2x.png";
        public string TemperatureDisplay => $"{Temperature}°C";
        public string WindSpeedDisplay => $"{WindSpeed} m/s";
        public string HumidityDisplay => $"{Humidity}%";
    }

    // OpenWeatherMap API Response Models
    public class OpenWeatherMapResponse
    {
        public MainData Main { get; set; } = new();
        public List<WeatherData> Weather { get; set; } = new();
        public WindData Wind { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }

    public class MainData
    {
        public double Temp { get; set; }
        public int Humidity { get; set; }
    }

    public class WeatherData
    {
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class WindData
    {
        public double Speed { get; set; }
    }

    // Weather Forecast Models
    public class WeatherForecast
    {
        public string City { get; set; } = string.Empty;
        public List<DailyWeather> DailyForecasts { get; set; } = new();
        public int Days => DailyForecasts.Count;
    }

    public class DailyWeather
    {
        public DateTime Date { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        
        public string DateDisplay => Date.ToString("dd/MM (ddd)", new System.Globalization.CultureInfo("vi-VN"));
        public string MinTempDisplay => $"{MinTemperature}°C";
        public string MaxTempDisplay => $"{MaxTemperature}°C";
        public string TempRangeDisplay => $"{MinTemperature}°C - {MaxTemperature}°C";
        public string IconUrl => $"https://openweathermap.org/img/wn/{Icon}@2x.png";
        public string HumidityDisplay => $"{Humidity}%";
        public string WindSpeedDisplay => $"{WindSpeed} m/s";
    }

    // OpenWeatherMap 5-day Forecast Response Models
    public class OpenWeatherMapForecastResponse
    {
        public List<ForecastItem> List { get; set; } = new();
        public CityInfo City { get; set; } = new();
    }

    public class ForecastItem
    {
        public long Dt { get; set; }
        public MainData Main { get; set; } = new();
        public List<WeatherData> Weather { get; set; } = new();
        public WindData Wind { get; set; } = new();
        public string Dt_txt { get; set; } = string.Empty;
    }

    public class CityInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
