using System.Text.Json;

namespace WebHS.Services
{
    public interface IUnsplashService
    {
        Task<List<UnsplashImage>> GetRandomImagesAsync(string query = "homestay", int count = 6);
        Task<UnsplashImage?> GetRandomImageAsync(string query = "travel");
    }

    public class UnsplashService : IUnsplashService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnsplashService> _logger;
        private readonly string _accessKey;
        private readonly string _baseUrl;

        public UnsplashService(HttpClient httpClient, IConfiguration configuration, ILogger<UnsplashService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _accessKey = configuration["ExternalAPIs:Unsplash:AccessKey"] ?? "";
            _baseUrl = configuration["ExternalAPIs:Unsplash:BaseUrl"] ?? "https://api.unsplash.com";
        }

        public async Task<List<UnsplashImage>> GetRandomImagesAsync(string query = "homestay", int count = 6)
        {
            if (string.IsNullOrEmpty(_accessKey))
            {
                _logger.LogWarning("Unsplash API key not configured");
                return GetFallbackImages();
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Client-ID {_accessKey}");

                var url = $"{_baseUrl}/photos/random?query={Uri.EscapeDataString(query)}&count={count}&orientation=landscape";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var unsplashData = JsonSerializer.Deserialize<List<UnsplashPhoto>>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (unsplashData != null && unsplashData.Any())
                    {
                        return unsplashData.Select(photo => new UnsplashImage
                        {
                            Id = photo.Id,
                            Url = photo.Urls.Regular,
                            ThumbUrl = photo.Urls.Thumb,
                            Description = photo.AltDescription ?? photo.Description ?? "Beautiful homestay",
                            AuthorName = photo.User.Name,
                            AuthorUrl = photo.User.Links.Html,
                            DownloadUrl = photo.Links.Download
                        }).ToList();
                    }
                }
                else
                {
                    _logger.LogWarning("Unsplash API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching images from Unsplash API");
            }

            return GetFallbackImages();
        }

        public async Task<UnsplashImage?> GetRandomImageAsync(string query = "travel")
        {
            var images = await GetRandomImagesAsync(query, 1);
            return images.FirstOrDefault();
        }

        private static List<UnsplashImage> GetFallbackImages()
        {
            return new List<UnsplashImage>
            {
                new() { Id = "fallback1", Url = "/images/homestay-1.jpg", ThumbUrl = "/images/homestay-1-thumb.jpg", Description = "Cozy homestay", AuthorName = "WebHS", AuthorUrl = "#" },
                new() { Id = "fallback2", Url = "/images/homestay-2.jpg", ThumbUrl = "/images/homestay-2-thumb.jpg", Description = "Beautiful view", AuthorName = "WebHS", AuthorUrl = "#" },
                new() { Id = "fallback3", Url = "/images/homestay-3.jpg", ThumbUrl = "/images/homestay-3-thumb.jpg", Description = "Modern interior", AuthorName = "WebHS", AuthorUrl = "#" },
                new() { Id = "fallback4", Url = "/images/homestay-4.jpg", ThumbUrl = "/images/homestay-4-thumb.jpg", Description = "Garden view", AuthorName = "WebHS", AuthorUrl = "#" },
                new() { Id = "fallback5", Url = "/images/homestay-5.jpg", ThumbUrl = "/images/homestay-5-thumb.jpg", Description = "Ocean view", AuthorName = "WebHS", AuthorUrl = "#" },
                new() { Id = "fallback6", Url = "/images/homestay-6.jpg", ThumbUrl = "/images/homestay-6-thumb.jpg", Description = "Mountain retreat", AuthorName = "WebHS", AuthorUrl = "#" }
            };
        }
    }

    // Data Transfer Objects
    public class UnsplashImage
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ThumbUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }

    // Unsplash API Response Models
    public class UnsplashPhoto
    {
        public string Id { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AltDescription { get; set; }
        public UnsplashUrls Urls { get; set; } = new();
        public UnsplashUser User { get; set; } = new();
        public UnsplashLinks Links { get; set; } = new();
    }

    public class UnsplashUrls
    {
        public string Raw { get; set; } = string.Empty;
        public string Full { get; set; } = string.Empty;
        public string Regular { get; set; } = string.Empty;
        public string Small { get; set; } = string.Empty;
        public string Thumb { get; set; } = string.Empty;
    }

    public class UnsplashUser
    {
        public string Name { get; set; } = string.Empty;
        public UnsplashUserLinks Links { get; set; } = new();
    }

    public class UnsplashUserLinks
    {
        public string Html { get; set; } = string.Empty;
    }

    public class UnsplashLinks
    {
        public string Download { get; set; } = string.Empty;
    }
}
