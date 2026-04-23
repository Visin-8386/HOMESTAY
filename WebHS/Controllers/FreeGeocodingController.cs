using Microsoft.AspNetCore.Mvc;
using WebHS.Models;
using WebHS.Services.Enhanced;

namespace WebHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FreeGeocodingController : ControllerBase
    {
        private readonly FreeGeocodingService _freeGeocodingService;
        private readonly ILogger<FreeGeocodingController> _logger;

        public FreeGeocodingController(
            FreeGeocodingService freeGeocodingService,
            ILogger<FreeGeocodingController> logger)
        {
            _freeGeocodingService = freeGeocodingService;
            _logger = logger;
        }

        /// <summary>
        /// Get coordinates from address using free geocoding services only
        /// (No Google Maps API key required)
        /// </summary>
        [HttpGet("coordinates")]
        [HttpPost("coordinates")]
        public async Task<IActionResult> GetCoordinates([FromQuery] string? address, [FromBody] FreeGeocodingRequest? request = null)
        {
            var addressToUse = address ?? request?.Address;
            
            if (string.IsNullOrWhiteSpace(addressToUse))
            {
                return BadRequest(new { success = false, message = "Address is required" });
            }

            try
            {
                _logger.LogInformation("🔍 Free geocoding request for address: {Address}", addressToUse);
                
                var result = await _freeGeocodingService.GeocodeAsync(addressToUse);

                if (result?.IsSuccess == true)
                {
                    _logger.LogInformation("✅ Free geocoding successful using: {Source}", result.Source);
                    
                    return Ok(new { 
                        success = true, 
                        latitude = result.Latitude, 
                        longitude = result.Longitude, 
                        displayName = result.FormattedAddress,
                        formattedAddress = result.FormattedAddress,
                        source = result.Source,
                        components = result.Components,
                        confidence = result.Confidence,
                        message = "Free geocoding completed successfully"
                    });
                }

                _logger.LogWarning("❌ Free geocoding failed for address: {Address}", addressToUse);
                return Ok(new { 
                    success = false, 
                    message = result?.ErrorMessage ?? "Coordinates not found for the given address using free services",
                    source = "Free Geocoding Services (Nominatim + Photon + Local Database)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in free geocoding for address: {Address}", addressToUse);
                return Ok(new { 
                    success = false, 
                    message = ex.Message,
                    error = "Free geocoding service error"
                });
            }
        }

        /// <summary>
        /// Get address suggestions from query using free services
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? query, [FromQuery] int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { success = false, message = "Query is required" });
            }

            try
            {
                _logger.LogInformation("🔍 Free geocoding suggestions for query: {Query}", query);
                
                var suggestions = await _freeGeocodingService.SearchSuggestionsAsync(query, maxResults);

                return Ok(new { 
                    success = true, 
                    suggestions = suggestions.Select(s => new {
                        latitude = s.Latitude,
                        longitude = s.Longitude,
                        formattedAddress = s.FormattedAddress,
                        source = s.Source,
                        components = s.Components,
                        confidence = s.Confidence
                    }),
                    count = suggestions.Count,
                    message = "Free geocoding suggestions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting free geocoding suggestions for query: {Query}", query);
                return Ok(new { 
                    success = false, 
                    message = ex.Message,
                    error = "Free geocoding suggestions error"
                });
            }
        }

        /// <summary>
        /// Test endpoint to verify free geocoding services
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestFreeServices([FromQuery] string address = "Hồ Chí Minh, Việt Nam")
        {
            try
            {
                _logger.LogInformation("🧪 Testing free geocoding services with address: {Address}", address);
                
                var result = await _freeGeocodingService.GeocodeAsync(address);
                
                return Ok(new { 
                    success = result?.IsSuccess ?? false,
                    testAddress = address,
                    result = new {
                        latitude = result?.Latitude,
                        longitude = result?.Longitude,
                        formattedAddress = result?.FormattedAddress,
                        source = result?.Source,
                        components = result?.Components,
                        confidence = result?.Confidence,
                        errorMessage = result?.ErrorMessage
                    },
                    servicesAvailable = new[] {
                        "Vietnam Local Database",
                        "Enhanced Nominatim",
                        "Photon (OSM-based)",
                        "Basic Nominatim"
                    },
                    message = "Free geocoding test completed",
                    note = "These services work without any API keys and are completely free"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing free geocoding services");
                return Ok(new { 
                    success = false, 
                    error = ex.Message,
                    message = "Free geocoding test failed"
                });
            }
        }
    }

    public class FreeGeocodingRequest
    {
        public string? Address { get; set; }
    }
}
