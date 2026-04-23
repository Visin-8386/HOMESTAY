using System.Text.Json;

namespace WebHS.Services
{
    public interface IGoogleStructuredDataService
    {
        string GenerateHomestayStructuredData(dynamic homestayData);
        string GenerateOrganizationStructuredData();
        string GenerateWebsiteStructuredData();
        string GenerateBreadcrumbStructuredData(List<BreadcrumbItem> breadcrumbs);
        string GenerateReviewStructuredData(dynamic reviewData);
        string GenerateEventStructuredData(dynamic eventData);
    }

    public class GoogleStructuredDataService : IGoogleStructuredDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleStructuredDataService> _logger;

        public GoogleStructuredDataService(IConfiguration configuration, ILogger<GoogleStructuredDataService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateHomestayStructuredData(dynamic homestayData)
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "LodgingBusiness",
                    name = homestayData.Name?.ToString(),
                    description = homestayData.Description?.ToString(),                    address = new
                    {
                        type = "PostalAddress",
                        streetAddress = homestayData.Address?.ToString(),
                        addressLocality = homestayData.City?.ToString(),
                        addressRegion = GetPropertyValue(homestayData, "State") ?? "",
                        postalCode = GetPropertyValue(homestayData, "ZipCode") ?? "",
                        addressCountry = "VN"
                    },
                    geo = homestayData.Latitude != null && homestayData.Longitude != null ? new
                    {
                        type = "GeoCoordinates",
                        latitude = homestayData.Latitude,
                        longitude = homestayData.Longitude
                    } : null,
                    url = $"{GetBaseUrl()}/Homestay/Details/{homestayData.Id}",
                    telephone = homestayData.PhoneNumber?.ToString(),
                    priceRange = FormatPriceRange(homestayData.PricePerNight),
                    starRating = homestayData.AverageRating != null ? new
                    {
                        type = "Rating",
                        ratingValue = homestayData.AverageRating,
                        bestRating = "5",
                        worstRating = "1"
                    } : null,
                    amenityFeature = GenerateAmenityFeatures(homestayData.Amenities),
                    numberOfRooms = homestayData.Bedrooms,
                    maximumAttendeeCapacity = homestayData.MaxGuests,
                    image = GenerateImageUrls(homestayData.Images),
                    aggregateRating = homestayData.TotalReviews > 0 ? new
                    {
                        type = "AggregateRating",
                        ratingValue = homestayData.AverageRating,
                        reviewCount = homestayData.TotalReviews,
                        bestRating = "5",
                        worstRating = "1"
                    } : null
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating homestay structured data");
                return string.Empty;
            }
        }

        public string GenerateOrganizationStructuredData()
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "Organization",
                    name = "WebHS - Homestay Booking Platform",
                    description = "Nền tảng đặt phòng homestay hàng đầu Việt Nam với hàng ngàn lựa chọn chất lượng",
                    url = GetBaseUrl(),
                    logo = $"{GetBaseUrl()}/images/logo.png",
                    sameAs = new[]
                    {
                        "https://www.facebook.com/webhs",
                        "https://www.instagram.com/webhs",
                        "https://www.youtube.com/webhs"
                    },
                    contactPoint = new
                    {
                        type = "ContactPoint",
                        telephone = "+84-123-456-789",
                        contactType = "customer service",
                        areaServed = "VN",
                        availableLanguage = new[] { "Vietnamese", "English" }
                    },
                    address = new
                    {
                        type = "PostalAddress",
                        addressLocality = "Ho Chi Minh City",
                        addressRegion = "Ho Chi Minh",
                        addressCountry = "VN"
                    }
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating organization structured data");
                return string.Empty;
            }
        }

        public string GenerateWebsiteStructuredData()
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "WebSite",
                    name = "WebHS - Homestay Booking",
                    url = GetBaseUrl(),
                    potentialAction = new
                    {
                        type = "SearchAction",
                        target = new
                        {
                            type = "EntryPoint",
                            urlTemplate = $"{GetBaseUrl()}/Homestay/Search?query={{search_term_string}}"
                        },
                        queryInput = "required name=search_term_string"
                    }
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating website structured data");
                return string.Empty;
            }
        }

        public string GenerateBreadcrumbStructuredData(List<BreadcrumbItem> breadcrumbs)
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "BreadcrumbList",
                    itemListElement = breadcrumbs.Select((item, index) => new
                    {
                        type = "ListItem",
                        position = index + 1,
                        name = item.Name,
                        item = item.Url
                    }).ToArray()
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating breadcrumb structured data");
                return string.Empty;
            }
        }

        public string GenerateReviewStructuredData(dynamic reviewData)
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "Review",
                    itemReviewed = new
                    {
                        type = "LodgingBusiness",
                        name = reviewData.HomestayName?.ToString()
                    },
                    reviewRating = new
                    {
                        type = "Rating",
                        ratingValue = reviewData.Rating,
                        bestRating = "5",
                        worstRating = "1"
                    },
                    name = reviewData.Title?.ToString(),
                    reviewBody = reviewData.Comment?.ToString(),
                    author = new
                    {
                        type = "Person",
                        name = reviewData.GuestName?.ToString()
                    },
                    datePublished = reviewData.CreatedAt?.ToString("yyyy-MM-dd")
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating review structured data");
                return string.Empty;
            }
        }

        public string GenerateEventStructuredData(dynamic eventData)
        {
            try
            {
                var structuredData = new
                {
                    context = "https://schema.org",
                    type = "Event",
                    name = eventData.Name?.ToString(),
                    description = eventData.Description?.ToString(),
                    startDate = eventData.StartDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    endDate = eventData.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    location = new
                    {
                        type = "Place",
                        name = eventData.LocationName?.ToString(),
                        address = eventData.Address?.ToString()
                    },
                    organizer = new
                    {
                        type = "Organization",
                        name = "WebHS"
                    }
                };

                return JsonSerializer.Serialize(structuredData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating event structured data");
                return string.Empty;
            }
        }

        private string GetBaseUrl()
        {
            return _configuration["BaseUrl"] ?? "https://localhost:7000";
        }

        private string FormatPriceRange(object? pricePerNight)
        {
            if (pricePerNight == null) return "$";
            
            var price = Convert.ToDecimal(pricePerNight);
            if (price < 100000) return "$";
            if (price < 300000) return "$$";
            if (price < 500000) return "$$$";
            return "$$$$";
        }

        private object[] GenerateAmenityFeatures(object? amenities)
        {
            if (amenities == null) return Array.Empty<object>();

            var amenityList = new List<object>();
            var amenityString = amenities.ToString();
            
            if (!string.IsNullOrEmpty(amenityString))
            {
                var amenityNames = amenityString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var amenity in amenityNames)
                {
                    amenityList.Add(new
                    {
                        type = "LocationFeatureSpecification",
                        name = amenity.Trim()
                    });
                }
            }

            return amenityList.ToArray();
        }

        private string[] GenerateImageUrls(object? images)
        {
            if (images == null) return Array.Empty<string>();

            var imageList = new List<string>();
            var baseUrl = GetBaseUrl();
            
            // This would need to be adapted based on your actual image storage structure
            imageList.Add($"{baseUrl}/images/homestay-default.jpg");            return imageList.ToArray();
        }

        private static string? GetPropertyValue(dynamic obj, string propertyName)
        {
            try
            {
                var value = obj.GetType().GetProperty(propertyName)?.GetValue(obj);
                return value?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    public class BreadcrumbItem
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
