using Microsoft.AspNetCore.Mvc;
using WebHS.Services;

namespace WebHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IHomestayService _homestayService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(
            IWeatherService weatherService, 
            IHomestayService homestayService,
            ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _homestayService = homestayService;
            _logger = logger;
        }

        /// <summary>
        /// Get weather information by coordinates (for homestay location)
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <returns>Weather information</returns>
        [HttpGet("coordinates")]
        public async Task<IActionResult> GetWeatherByCoordinates(double latitude, double longitude)
        {
            try
            {
                if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180." 
                    });
                }

                var weather = await _weatherService.GetWeatherAsync(latitude, longitude);
                
                // WeatherService now always returns data (mock if API fails)
                return Ok(new { 
                    success = true, 
                    data = weather 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for coordinates: {Lat}, {Lon}", latitude, longitude);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather information by city name
        /// </summary>
        /// <param name="city">City name</param>
        /// <returns>Weather information</returns>
        [HttpGet("city/{city}")]
        public async Task<IActionResult> GetWeatherByCity(string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "City name is required" 
                    });
                }

                var weather = await _weatherService.GetWeatherAsync(city);
                
                // WeatherService now always returns data (mock if API fails)
                return Ok(new { 
                    success = true, 
                    data = weather 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for city: {City}", city);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather for homestay location (combines with homestay data)
        /// </summary>
        /// <param name="homestayId">Homestay ID</param>
        /// <returns>Weather information for homestay location</returns>
        [HttpGet("homestay/{homestayId}")]
        public async Task<IActionResult> GetWeatherForHomestay(int homestayId)
        {
            try
            {
                // Get homestay details first
                var homestay = await _homestayService.GetHomestayByIdAsync(homestayId);
                
                if (homestay == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Homestay not found" 
                    });
                }

                WeatherInfo weather;

                // Try to get weather by coordinates first (more accurate)
                if (homestay.Latitude != 0 && homestay.Longitude != 0)
                {
                    weather = await _weatherService.GetWeatherAsync(
                        (double)homestay.Latitude, 
                        (double)homestay.Longitude);
                }
                else if (!string.IsNullOrEmpty(homestay.City))
                {
                    // Fallback to city name
                    weather = await _weatherService.GetWeatherAsync(homestay.City);
                }
                else
                {
                    // Last resort - use generic mock data
                    weather = await _weatherService.GetWeatherAsync("Việt Nam");
                }

                return Ok(new { 
                    success = true, 
                    data = weather,
                    homestay = new {
                        id = homestay.Id,
                        name = homestay.Name,
                        city = homestay.City,
                        address = homestay.Address
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data for homestay: {HomestayId}", homestayId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather data at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather forecast by coordinates
        /// </summary>
        /// <param name="latitude">Latitude coordinate</param>
        /// <param name="longitude">Longitude coordinate</param>
        /// <param name="days">Number of days to forecast (default: 5, max: 5)</param>
        /// <returns>Weather forecast information</returns>
        [HttpGet("forecast/coordinates")]
        public async Task<IActionResult> GetForecastByCoordinates(double latitude, double longitude, int days = 5)
        {
            try
            {
                if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180." 
                    });
                }

                if (days < 1 || days > 5)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Days must be between 1 and 5." 
                    });
                }

                var forecast = await _weatherService.GetForecastAsync(latitude, longitude, days);
                
                return Ok(new { 
                    success = true, 
                    data = forecast 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast for coordinates: {Lat}, {Lon}", latitude, longitude);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather forecast at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather forecast by city name
        /// </summary>
        /// <param name="city">City name</param>
        /// <param name="days">Number of days to forecast (default: 5, max: 5)</param>
        /// <returns>Weather forecast information</returns>
        [HttpGet("forecast/city/{city}")]
        public async Task<IActionResult> GetForecastByCity(string city, int days = 5)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "City name is required" 
                    });
                }

                if (days < 1 || days > 5)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Days must be between 1 and 5." 
                    });
                }

                var forecast = await _weatherService.GetForecastAsync(city, days);
                
                return Ok(new { 
                    success = true, 
                    data = forecast 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast for city: {City}", city);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather forecast at this time" 
                });
            }
        }

        /// <summary>
        /// Get weather forecast for homestay location
        /// </summary>
        /// <param name="homestayId">Homestay ID</param>
        /// <param name="days">Number of days to forecast (default: 5, max: 5)</param>
        /// <returns>Weather forecast information for homestay location</returns>
        [HttpGet("forecast/homestay/{homestayId}")]
        public async Task<IActionResult> GetForecastForHomestay(int homestayId, int days = 5)
        {
            try
            {
                if (days < 1 || days > 5)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Days must be between 1 and 5." 
                    });
                }

                // Get homestay details first
                var homestay = await _homestayService.GetHomestayByIdAsync(homestayId);
                
                if (homestay == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Homestay not found" 
                    });
                }

                WeatherForecast forecast;

                // Try to get forecast by coordinates first (more accurate)
                if (homestay.Latitude != 0 && homestay.Longitude != 0)
                {
                    forecast = await _weatherService.GetForecastAsync(
                        (double)homestay.Latitude, 
                        (double)homestay.Longitude, 
                        days);
                }
                else if (!string.IsNullOrEmpty(homestay.City))
                {
                    // Fallback to city name
                    forecast = await _weatherService.GetForecastAsync(homestay.City, days);
                }
                else
                {
                    // Last resort - use generic mock data
                    forecast = await _weatherService.GetForecastAsync("Việt Nam", days);
                }

                return Ok(new { 
                    success = true, 
                    data = forecast,
                    homestay = new {
                        id = homestay.Id,
                        name = homestay.Name,
                        city = homestay.City,
                        address = homestay.Address
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather forecast for homestay: {HomestayId}", homestayId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Unable to fetch weather forecast at this time" 
                });
            }
        }
    }
}
