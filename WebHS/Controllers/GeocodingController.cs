using Microsoft.AspNetCore.Mvc;
using WebHS.Services;

namespace WebHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeocodingController : ControllerBase
    {
        private readonly GeocodingService _geocodingService;

        public GeocodingController(GeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        [HttpGet("coordinates")]
        [HttpPost("coordinates")]
        public async Task<IActionResult> GetCoordinates([FromQuery] string? address, [FromBody] GeocodingRequest? request = null)
        {
            // Handle both query parameter and request body
            var addressToUse = address ?? request?.Address;

            if (string.IsNullOrWhiteSpace(addressToUse))
            {
                return BadRequest(new { success = false, message = "Address is required" });
            }

            try
            {
                var (latitude, longitude) = await _geocodingService.GetCoordinatesAsync(addressToUse);

                if (latitude.HasValue && longitude.HasValue)
                {
                    return Ok(new
                    {
                        success = true,
                        latitude = latitude.Value,
                        longitude = longitude.Value,
                        displayName = addressToUse
                    });
                }

                return Ok(new { success = false, message = "Coordinates not found for the given address" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("address")]
        [HttpPost("address")]
        public async Task<IActionResult> GetAddress([FromQuery] double? latitude, [FromQuery] double? longitude, [FromBody] ReverseGeocodingRequest? request = null)
        {
            // Handle both query parameters and request body
            var lat = latitude ?? request?.Latitude;
            var lng = longitude ?? request?.Longitude;

            if (!lat.HasValue || !lng.HasValue)
            {
                return BadRequest(new { success = false, message = "Latitude and longitude are required" });
            }

            try
            {
                var address = await _geocodingService.GetAddressFromCoordinatesAsync(lat.Value, lng.Value);

                if (!string.IsNullOrEmpty(address))
                {
                    return Ok(new
                    {
                        success = true,
                        address = address,
                        latitude = lat.Value,
                        longitude = lng.Value
                    });
                }

                return Ok(new { success = false, message = "Address not found for the given coordinates" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("reverse")]
        [HttpPost("reverse")]
        public async Task<IActionResult> ReverseGeocode([FromQuery] double? lat, [FromQuery] double? lng, [FromBody] ReverseGeocodingRequest? request = null)
        {
            // Handle both query parameters and request body
            var latitude = lat ?? request?.Latitude;
            var longitude = lng ?? request?.Longitude;

            if (!latitude.HasValue || !longitude.HasValue)
            {
                return BadRequest(new { success = false, message = "Latitude and longitude are required" });
            }

            try
            {
                var address = await _geocodingService.GetAddressFromCoordinatesAsync(latitude.Value, longitude.Value);

                if (!string.IsNullOrEmpty(address))
                {
                    return Ok(new
                    {
                        success = true,
                        address = address,
                        latitude = latitude.Value,
                        longitude = longitude.Value,
                        displayName = address
                    });
                }

                return Ok(new { success = false, message = "Address not found for the given coordinates" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> BatchGeocode([FromBody] BatchGeocodingRequest request)
        {
            if (request?.Addresses == null || !request.Addresses.Any())
            {
                return BadRequest(new { success = false, message = "Addresses array is required" });
            }

            try
            {
                var results = new List<object>();

                foreach (var address in request.Addresses.Take(10)) // Limit to 10 addresses
                {
                    try
                    {
                        var (latitude, longitude) = await _geocodingService.GetCoordinatesAsync(address);

                        results.Add(new
                        {
                            address = address,
                            success = latitude.HasValue && longitude.HasValue,
                            latitude = latitude,
                            longitude = longitude
                        });
                    }
                    catch
                    {
                        results.Add(new
                        {
                            address = address,
                            success = false,
                            latitude = (double?)null,
                            longitude = (double?)null
                        });
                    }
                }

                return Ok(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        public class GeocodingRequest
        {
            public string? Address { get; set; }
        }

        public class ReverseGeocodingRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class BatchGeocodingRequest
        {
            public string[]? Addresses { get; set; }
        }
    }
}
