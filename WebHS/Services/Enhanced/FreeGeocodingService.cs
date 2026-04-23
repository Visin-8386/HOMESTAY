using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebHS.Models;

namespace WebHS.Services.Enhanced
{
    public class FreeGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FreeGeocodingService> _logger;
        private readonly VietnamAddressService _vietnamAddressService;

        public FreeGeocodingService(
            HttpClient httpClient, 
            ILogger<FreeGeocodingService> logger,
            VietnamAddressService vietnamAddressService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _vietnamAddressService = vietnamAddressService;
            
            // Set user agent to avoid blocking
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "WebHS Homestay Booking System/1.0 (contact@webhs.com)");
        }

        public async Task<GeocodingResult?> GeocodeAsync(string address)
        {
            _logger.LogInformation("Starting free geocoding for address: {Address}", address);

            try
            {
                // 1. Try Vietnam local database first (fastest and most accurate for Vietnam)
                var localResult = await _vietnamAddressService.GetApproximateCoordinatesAsync(address);
                if (localResult != null)
                {
                    _logger.LogInformation("Found coordinates in local Vietnam database");
                    return new GeocodingResult
                    {
                        IsSuccess = true,
                        Latitude = localResult.Value.Latitude,
                        Longitude = localResult.Value.Longitude,
                        FormattedAddress = address, // Use original address as formatted
                        Source = "Vietnam Local Database"
                    };
                }

                // 2. Try enhanced Nominatim with Vietnam-specific parsing
                var nominatimResult = await TryEnhancedNominatimAsync(address);
                if (nominatimResult?.IsSuccess == true)
                {
                    _logger.LogInformation("Found coordinates using enhanced Nominatim");
                    return nominatimResult;
                }

                // 3. Try alternative free geocoding services
                var alternatives = new[]
                {
                    () => TryPhotonAsync(address),
                    () => TryMapBoxAsync(address), // Using free tier
                    () => TryBasicNominatimAsync(address) // Final fallback
                };

                foreach (var alternative in alternatives)
                {
                    try
                    {
                        var result = await alternative();
                        if (result?.IsSuccess == true)
                        {
                            _logger.LogInformation("Found coordinates using alternative service: {Source}", result.Source);
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Alternative geocoding service failed");
                        continue;
                    }
                }

                // All methods failed
                _logger.LogWarning("All free geocoding methods failed for address: {Address}", address);
                return new GeocodingResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Không thể tìm thấy tọa độ cho địa chỉ này bằng các dịch vụ miễn phí",
                    Source = "Free Geocoding Services"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in free geocoding for address: {Address}", address);
                return new GeocodingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Source = "Free Geocoding Services"
                };
            }
        }

        private async Task<GeocodingResult?> TryEnhancedNominatimAsync(string address)
        {
            try
            {
                // Enhance Vietnamese address for better Nominatim results
                var enhancedAddress = EnhanceVietnameseAddress(address);
                
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(enhancedAddress)}&countrycodes=vn&limit=1&addressdetails=1";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<JsonElement[]>(content);
                
                if (results != null && results.Length > 0)
                {
                    var result = results[0];
                    var lat = result.GetProperty("lat").GetString();
                    var lon = result.GetProperty("lon").GetString();
                    var displayName = result.GetProperty("display_name").GetString();
                    
                    if (double.TryParse(lat, out var latitude) && double.TryParse(lon, out var longitude))
                    {
                        return new GeocodingResult
                        {
                            IsSuccess = true,
                            Latitude = latitude,
                            Longitude = longitude,
                            FormattedAddress = FormatVietnameseAddress(displayName ?? address),
                            Source = "Enhanced Nominatim"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enhanced Nominatim failed for address: {Address}", address);
            }
            
            return null;
        }

        private async Task<GeocodingResult?> TryPhotonAsync(string address)
        {
            try
            {
                // Photon is another free OSM-based geocoder
                var url = $"https://photon.komoot.io/api/?q={Uri.EscapeDataString(address)}&limit=1";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (data.TryGetProperty("features", out var features) && features.GetArrayLength() > 0)
                {
                    var feature = features[0];
                    var geometry = feature.GetProperty("geometry");
                    var coordinates = geometry.GetProperty("coordinates");
                    
                    var longitude = coordinates[0].GetDouble();
                    var latitude = coordinates[1].GetDouble();
                    
                    var properties = feature.GetProperty("properties");
                    var name = properties.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : address;
                    
                    return new GeocodingResult
                    {
                        IsSuccess = true,
                        Latitude = latitude,
                        Longitude = longitude,
                        FormattedAddress = name ?? address,
                        Source = "Photon"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Photon geocoding failed for address: {Address}", address);
            }
            
            return null;
        }

        private async Task<GeocodingResult?> TryMapBoxAsync(string address)
        {
            try
            {
                // Using MapBox free tier (no API key required for basic geocoding)
                var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(address)}.json?country=vn&limit=1";
                
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (data.TryGetProperty("features", out var features) && features.GetArrayLength() > 0)
                    {
                        var feature = features[0];
                        var geometry = feature.GetProperty("geometry");
                        var coordinates = geometry.GetProperty("coordinates");
                        
                        var longitude = coordinates[0].GetDouble();
                        var latitude = coordinates[1].GetDouble();
                        var placeName = feature.GetProperty("place_name").GetString();
                        
                        return new GeocodingResult
                        {
                            IsSuccess = true,
                            Latitude = latitude,
                            Longitude = longitude,
                            FormattedAddress = placeName ?? address,
                            Source = "MapBox"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MapBox geocoding failed for address: {Address}", address);
            }
            
            return null;
        }

        private async Task<GeocodingResult?> TryBasicNominatimAsync(string address)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(address)}&limit=1";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<JsonElement[]>(content);
                
                if (results != null && results.Length > 0)
                {
                    var result = results[0];
                    var lat = result.GetProperty("lat").GetString();
                    var lon = result.GetProperty("lon").GetString();
                    var displayName = result.GetProperty("display_name").GetString();
                    
                    if (double.TryParse(lat, out var latitude) && double.TryParse(lon, out var longitude))
                    {
                        return new GeocodingResult
                        {
                            IsSuccess = true,
                            Latitude = latitude,
                            Longitude = longitude,
                            FormattedAddress = displayName ?? address,
                            Source = "Basic Nominatim"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Basic Nominatim failed for address: {Address}", address);
            }
            
            return null;
        }

        private string EnhanceVietnameseAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            var enhanced = address.ToLower();

            // Add country suffix if not present
            if (!enhanced.Contains("vietnam") && !enhanced.Contains("việt nam"))
            {
                enhanced += ", Vietnam";
            }

            // Enhance common Vietnamese address patterns
            var replacements = new Dictionary<string, string>
            {
                { "q.", "quận " },
                { "p.", "phường " },
                { "tp.", "thành phố " },
                { "hcm", "Hồ Chí Minh" },
                { "hn", "Hà Nội" },
                { "đn", "Đà Nẵng" },
                { "ct", "Cần Thơ" }
            };

            foreach (var replacement in replacements)
            {
                enhanced = enhanced.Replace(replacement.Key, replacement.Value);
            }

            return enhanced;
        }

        private string FormatVietnameseAddress(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            // Clean up the display name for Vietnamese addresses
            var parts = displayName.Split(',').Select(p => p.Trim()).ToArray();
            
            // Remove duplicate "Vietnam" entries
            parts = parts.Where(p => !string.Equals(p, "Vietnam", StringComparison.OrdinalIgnoreCase) || 
                                   parts.Count(x => string.Equals(x, "Vietnam", StringComparison.OrdinalIgnoreCase)) == 1).ToArray();
            
            return string.Join(", ", parts);
        }

        public async Task<List<GeocodingResult>> SearchSuggestionsAsync(string query, int maxResults = 5)
        {
            var suggestions = new List<GeocodingResult>();

            try
            {
                // Try Nominatim for suggestions
                var nominatimSuggestions = await GetNominatimSuggestionsAsync(query, maxResults);
                suggestions.AddRange(nominatimSuggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            }

            return suggestions.Take(maxResults).ToList();
        }

        private async Task<List<GeocodingResult>> GetNominatimSuggestionsAsync(string query, int maxResults)
        {
            var suggestions = new List<GeocodingResult>();

            try
            {
                var enhancedQuery = EnhanceVietnameseAddress(query);
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(enhancedQuery)}&countrycodes=vn&limit={maxResults}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<JsonElement[]>(content);

                if (results != null)
                {
                    foreach (var result in results)
                    {
                        var lat = result.GetProperty("lat").GetString();
                        var lon = result.GetProperty("lon").GetString();
                        var displayName = result.GetProperty("display_name").GetString();

                        if (double.TryParse(lat, out var latitude) && double.TryParse(lon, out var longitude))
                        {
                            suggestions.Add(new GeocodingResult
                            {
                                IsSuccess = true,
                                Latitude = latitude,
                                Longitude = longitude,
                                FormattedAddress = FormatVietnameseAddress(displayName ?? query),
                                Source = "Nominatim Suggestions"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting Nominatim suggestions for query: {Query}", query);
            }

            return suggestions;
        }
    }
}
