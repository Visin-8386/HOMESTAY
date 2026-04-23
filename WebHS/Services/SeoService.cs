using WebHS.Models;

namespace WebHS.Services
{
    public class SeoService : ISeoService
    {
        private readonly IConfiguration _configuration;

        public SeoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<string> GenerateMetaTitleAsync(string pageType, object? data = null)
        {
            var title = pageType.ToLower() switch
            {
                "home" => "WebHS - Premium Homestay Booking Platform",
                "homestays" => "Find Amazing Homestays - WebHS",
                "homestay-detail" when data is Homestay homestay => $"{homestay.Name} - WebHS",
                "booking" => "Book Your Perfect Homestay - WebHS",
                "host" => "Become a Host - WebHS",
                _ => "WebHS - Your Homestay Booking Solution"
            };

            return Task.FromResult(title);
        }

        public Task<string> GenerateMetaDescriptionAsync(string pageType, object? data = null)
        {
            var description = pageType.ToLower() switch
            {
                "home" => "Discover unique homestays worldwide. Book authentic local accommodations with trusted hosts. Start your unforgettable journey with WebHS.",
                "homestays" => "Browse thousands of verified homestays. Find the perfect accommodation for your next trip with our easy booking platform.",
                "homestay-detail" when data is Homestay homestay => $"Stay at {homestay.Name}. {homestay.Description?[..Math.Min(homestay.Description.Length, 150)]}...",
                "booking" => "Secure your homestay booking in just a few clicks. Instant confirmation and 24/7 support included.",
                "host" => "Join thousands of hosts earning extra income by sharing their homes. List your property today and start hosting.",
                _ => "WebHS - Your trusted platform for homestay bookings and authentic travel experiences."
            };

            return Task.FromResult(description);
        }

        public Task<string> GenerateMetaKeywordsAsync(string pageType, object? data = null)
        {
            var keywords = pageType.ToLower() switch
            {
                "home" => "homestay, booking, accommodation, travel, vacation rental, local experience",
                "homestays" => "homestay search, find accommodation, book homestay, vacation rental",
                "homestay-detail" => "homestay booking, accommodation details, vacation rental, local stay",
                "booking" => "homestay booking, reservation, accommodation booking, travel booking",
                "host" => "become host, list property, homestay hosting, earn money, vacation rental host",
                _ => "homestay, booking platform, accommodation, travel"
            };

            return Task.FromResult(keywords);
        }

        public Task<string> GenerateCanonicalUrlAsync(string pageType, object? data = null)
        {
            var baseUrl = _configuration["BaseUrl"] ?? "https://webhs.com";
            
            var canonicalUrl = pageType.ToLower() switch
            {
                "home" => baseUrl,
                "homestays" => $"{baseUrl}/homestays",
                "homestay-detail" when data is Homestay homestay => $"{baseUrl}/homestays/{homestay.Id}",
                "booking" => $"{baseUrl}/booking",
                "host" => $"{baseUrl}/host",
                _ => baseUrl
            };

            return Task.FromResult(canonicalUrl);
        }

        public Task<Dictionary<string, string>> GenerateStructuredDataAsync(string pageType, object? data = null)
        {
            var structuredData = new Dictionary<string, string>();

            switch (pageType.ToLower())
            {
                case "homestay-detail" when data is Homestay homestay:
                    structuredData["@context"] = "https://schema.org";
                    structuredData["@type"] = "LodgingBusiness";
                    structuredData["name"] = homestay.Name;
                    structuredData["description"] = homestay.Description ?? "";
                    structuredData["address"] = $"{homestay.Address}, {homestay.Ward}, {homestay.District}, {homestay.City}";
                    if (homestay.PricePerNight > 0)
                    {
                        structuredData["priceRange"] = $"${homestay.PricePerNight}/night";
                    }
                    break;

                default:
                    structuredData["@context"] = "https://schema.org";
                    structuredData["@type"] = "Organization";
                    structuredData["name"] = "WebHS";
                    structuredData["description"] = "Premium homestay booking platform";
                    break;
            }

            return Task.FromResult(structuredData);
        }
    }
}
