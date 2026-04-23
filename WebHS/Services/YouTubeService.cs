namespace WebHS.Services
{
    public interface IYouTubeService
    {
        Task<object> GetTravelVideosAsync(string city = "Vietnam", int maxResults = 5);
        Task<object> GetPlaylistVideosAsync(string playlistId, int maxResults = 10);
        object GetEmbedHtml(string videoId, int width = 560, int height = 315);
    }

    public class YouTubeService : IYouTubeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<YouTubeService> _logger;

        public YouTubeService(HttpClient httpClient, IConfiguration configuration, ILogger<YouTubeService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<object> GetTravelVideosAsync(string city = "Vietnam", int maxResults = 5)
        {
            var youtubeApiKey = _configuration["YouTube:ApiKey"];
            if (string.IsNullOrEmpty(youtubeApiKey) || youtubeApiKey == "YOUR_YOUTUBE_API_KEY_HERE")
            {
                // Return demo videos if no API key
                return GetDemoTravelVideos(city);
            }

            try
            {
                var searchQuery = $"{city} travel guide homestay";
                var url = $"https://www.googleapis.com/youtube/v3/search" +
                         $"?part=snippet" +
                         $"&q={Uri.EscapeDataString(searchQuery)}" +
                         $"&type=video" +
                         $"&maxResults={maxResults}" +
                         $"&order=relevance" +
                         $"&regionCode=VN" +
                         $"&relevanceLanguage=vi" +
                         $"&key={youtubeApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                
                var videos = new List<object>();
                if (data.TryGetProperty("items", out System.Text.Json.JsonElement items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("snippet", out System.Text.Json.JsonElement snippet) &&
                            item.TryGetProperty("id", out System.Text.Json.JsonElement id) &&
                            id.TryGetProperty("videoId", out System.Text.Json.JsonElement videoId))
                        {
                            videos.Add(new
                            {
                                videoId = videoId.GetString(),
                                title = snippet.TryGetProperty("title", out var title) ? title.GetString() : "",
                                description = snippet.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                thumbnail = snippet.TryGetProperty("thumbnails", out var thumbs) &&
                                           thumbs.TryGetProperty("medium", out var medium) &&
                                           medium.TryGetProperty("url", out var thumbUrl) ? thumbUrl.GetString() : "",
                                channelTitle = snippet.TryGetProperty("channelTitle", out var channel) ? channel.GetString() : "",
                                publishedAt = snippet.TryGetProperty("publishedAt", out var published) ? published.GetString() : ""
                            });
                        }
                    }
                }

                return new { success = true, videos = videos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching YouTube videos");
                // Fallback to demo videos if API fails
                return GetDemoTravelVideos(city);
            }
        }

        public async Task<object> GetPlaylistVideosAsync(string playlistId, int maxResults = 10)
        {
            var youtubeApiKey = _configuration["YouTube:ApiKey"];
            if (string.IsNullOrEmpty(youtubeApiKey) || youtubeApiKey == "YOUR_YOUTUBE_API_KEY_HERE")
            {
                return GetDemoTravelVideos("Vietnam");
            }

            try
            {
                var url = $"https://www.googleapis.com/youtube/v3/playlistItems" +
                         $"?part=snippet" +
                         $"&playlistId={playlistId}" +
                         $"&maxResults={maxResults}" +
                         $"&key={youtubeApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                
                var videos = new List<object>();
                if (data.TryGetProperty("items", out System.Text.Json.JsonElement items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("snippet", out System.Text.Json.JsonElement snippet) &&
                            snippet.TryGetProperty("resourceId", out System.Text.Json.JsonElement resourceId) &&
                            resourceId.TryGetProperty("videoId", out System.Text.Json.JsonElement videoId))
                        {
                            videos.Add(new
                            {
                                videoId = videoId.GetString(),
                                title = snippet.TryGetProperty("title", out var title) ? title.GetString() : "",
                                description = snippet.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                thumbnail = snippet.TryGetProperty("thumbnails", out var thumbs) &&
                                           thumbs.TryGetProperty("medium", out var medium) &&
                                           medium.TryGetProperty("url", out var thumbUrl) ? thumbUrl.GetString() : "",
                                channelTitle = snippet.TryGetProperty("channelTitle", out var channel) ? channel.GetString() : "",
                                publishedAt = snippet.TryGetProperty("publishedAt", out var published) ? published.GetString() : ""
                            });
                        }
                    }
                }

                return new { success = true, videos = videos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching playlist videos");
                return GetDemoTravelVideos("Vietnam");
            }
        }

        public object GetEmbedHtml(string videoId, int width = 560, int height = 315)
        {
            if (string.IsNullOrEmpty(videoId))
            {
                return new { success = false, error = "Video ID is required" };
            }

            var embedHtml = $@"
                <div class='youtube-embed-container' style='position: relative; padding-bottom: {(height * 100.0 / width):F1}%; height: 0; overflow: hidden;'>
                    <iframe 
                        style='position: absolute; top: 0; left: 0; width: 100%; height: 100%;'
                        src='https://www.youtube.com/embed/{videoId}?rel=0&showinfo=0&modestbranding=1'
                        frameborder='0'
                        allow='accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture'
                        allowfullscreen>
                    </iframe>
                </div>";

            return new { success = true, embedHtml = embedHtml };
        }

        private object GetDemoTravelVideos(string city)
        {
            var demoVideos = new List<object>
            {
                new {
                    videoId = "2MUbWLttOKg",
                    title = "Vietnam Travel Guide - Beautiful Destinations",
                    description = "Discover the most beautiful places in Vietnam for your homestay vacation",
                    thumbnail = "https://img.youtube.com/vi/2MUbWLttOKg/mqdefault.jpg",
                    channelTitle = "Travel Guide Vietnam",
                    publishedAt = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd")
                },
                new {
                    videoId = "CjZxj5tL_W0", 
                    title = "Ho Chi Minh City - Must Visit Places",
                    description = "Top attractions and homestays in Ho Chi Minh City",
                    thumbnail = "https://img.youtube.com/vi/CjZxj5tL_W0/mqdefault.jpg",
                    channelTitle = "Vietnam Tourism",
                    publishedAt = DateTime.Now.AddDays(-15).ToString("yyyy-MM-dd")
                },
                new {
                    videoId = "6qZWMNW7GmE",
                    title = "Hanoi Old Quarter Walking Tour",
                    description = "Experience the charm of Hanoi's historic streets and local homestays",
                    thumbnail = "https://img.youtube.com/vi/6qZWMNW7GmE/mqdefault.jpg", 
                    channelTitle = "Hanoi Explorer",
                    publishedAt = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")
                }
            };

            return new { success = true, videos = demoVideos, isDemo = true };
        }
    }
}
