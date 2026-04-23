using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebHS.Controllers
{
    public class YouTubeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public YouTubeController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        private async Task<object> GetTravelVideosData(string city, int maxResults)
        {
            var youtubeApiKey = _configuration["YouTube:ApiKey"];
            if (string.IsNullOrEmpty(youtubeApiKey) || youtubeApiKey == "YOUR_YOUTUBE_API_KEY_HERE")
            {
                return new { success = true, videos = GetDemoTravelVideos(city), isDemo = true };
            }

            try
            {
                var searchQuery = $"{city} travel guide homestay tourist attractions";
                var url = $"https://www.googleapis.com/youtube/v3/search" +
                         $"?part=snippet" +
                         $"&q={Uri.EscapeDataString(searchQuery)}" +
                         $"&type=video" +
                         $"&maxResults={maxResults}" +
                         $"&order=relevance" +
                         $"&regionCode=VN" +
                         $"&videoDuration=medium" +
                         $"&videoDefinition=high" +
                         $"&safeSearch=moderate" +
                         $"&key={youtubeApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<JsonElement>(response);
                
                var videos = new List<object>();
                if (data.TryGetProperty("items", out JsonElement items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("snippet", out JsonElement snippet) &&
                            item.TryGetProperty("id", out JsonElement id) &&
                            id.TryGetProperty("videoId", out JsonElement videoId))
                        {
                            videos.Add(new
                            {
                                videoId = videoId.GetString(),
                                title = snippet.TryGetProperty("title", out var title) ? title.GetString() : "",
                                description = snippet.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                thumbnailUrl = snippet.TryGetProperty("thumbnails", out var thumbs) &&
                                           thumbs.TryGetProperty("medium", out var medium) &&
                                           medium.TryGetProperty("url", out var thumbUrl) ? thumbUrl.GetString() : "",
                                channelTitle = snippet.TryGetProperty("channelTitle", out var channel) ? channel.GetString() : "",
                                publishedAt = snippet.TryGetProperty("publishedAt", out var published) ? published.GetString() : ""
                            });
                        }
                    }
                }

                return new { success = true, videos = videos, isDemo = false };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YouTube API Error: {ex.Message}");
                return new { success = true, videos = GetDemoTravelVideos(city), isDemo = true, error = "API temporarily unavailable" };
            }
        }

        private List<object> GetDemoTravelVideos(string city)
        {
            var demoVideos = new List<object>
            {
                new {
                    videoId = "2MUbWLttOKg",
                    title = $"{city} Travel Guide - Beautiful Destinations",
                    description = $"Discover the most beautiful places in {city} for your homestay vacation",
                    thumbnailUrl = "https://img.youtube.com/vi/2MUbWLttOKg/mqdefault.jpg",
                    channelTitle = "Travel Guide Vietnam",
                    publishedAt = DateTime.Now.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                new {
                    videoId = "CjZxj5tL_W0", 
                    title = $"{city} - Must Visit Places",
                    description = $"Top attractions and homestays in {city}",
                    thumbnailUrl = "https://img.youtube.com/vi/CjZxj5tL_W0/mqdefault.jpg",
                    channelTitle = "Amazing Vietnam",
                    publishedAt = DateTime.Now.AddDays(-15).ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                new {
                    videoId = "qVdPh2cBTN0",
                    title = $"Best Homestays in {city}",
                    description = $"Experience authentic local culture with these amazing homestays in {city}",
                    thumbnailUrl = "https://img.youtube.com/vi/qVdPh2cBTN0/mqdefault.jpg",
                    channelTitle = "Homestay Guide",
                    publishedAt = DateTime.Now.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            return demoVideos.OrderBy(x => Guid.NewGuid()).Take(6).ToList();
        }

        [HttpGet]
        public IActionResult Embed(string videoId, int width = 560, int height = 315)
        {
            if (string.IsNullOrEmpty(videoId))
            {
                // Luôn trả về JSON thay vì BadRequest
                return Json(new { success = false, error = "Video ID is required" });
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

            return Json(new { success = true, embedHtml = embedHtml });
        }

        [HttpGet]
        public IActionResult HomestayVideos()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHomestayVideos(string location = "Vietnam", string category = "all", int maxResults = 12)
        {
            var youtubeApiKey = _configuration["YouTube:ApiKey"];
            if (string.IsNullOrEmpty(youtubeApiKey) || youtubeApiKey == "YOUR_YOUTUBE_API_KEY_HERE")
            {
                return Json(new { success = true, videos = GetDemoHomestayVideos(location, category), isDemo = true });
            }

            try
            {
                var searchQuery = BuildHomestaySearchQuery(location, category);
                var url = $"https://www.googleapis.com/youtube/v3/search" +
                         $"?part=snippet" +
                         $"&q={Uri.EscapeDataString(searchQuery)}" +
                         $"&type=video" +
                         $"&maxResults={maxResults}" +
                         $"&order=relevance" +
                         $"&regionCode=VN" +
                         $"&videoDuration=medium" +
                         $"&videoDefinition=high" +
                         $"&safeSearch=moderate" +
                         $"&key={youtubeApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<JsonElement>(response);
                
                var videos = new List<object>();
                if (data.TryGetProperty("items", out JsonElement items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("snippet", out JsonElement snippet) &&
                            item.TryGetProperty("id", out JsonElement id) &&
                            id.TryGetProperty("videoId", out JsonElement videoId))
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
                                publishedAt = snippet.TryGetProperty("publishedAt", out var published) ? published.GetString() : "",
                                category = category
                            });
                        }
                    }
                }

                return Json(new { success = true, videos = videos, isDemo = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YouTube Homestay API Error: {ex.Message}");
                return Json(new { success = true, videos = GetDemoHomestayVideos(location, category), isDemo = true, error = "API temporarily unavailable" });
            }
        }

        private string BuildHomestaySearchQuery(string location, string category)
        {
            var baseQuery = $"{location} homestay";
            
            return category.ToLower() switch
            {
                "tour" => $"{baseQuery} tour review experience",
                "food" => $"{baseQuery} food cooking local cuisine",
                "culture" => $"{baseQuery} culture traditional life",
                "nature" => $"{baseQuery} nature mountain beach countryside",
                "family" => $"{baseQuery} family kids children friendly",
                "luxury" => $"{baseQuery} luxury villa resort beautiful",
                _ => $"{baseQuery} accommodation travel guide"
            };
        }

        private List<object> GetDemoHomestayVideos(string location, string category)
        {
            var demoVideos = new List<object>
            {
                new {
                    videoId = "dQw4w9WgXcQ",
                    title = $"Homestay tuyệt vời tại {location} - Trải nghiệm khó quên",
                    description = $"Khám phá homestay độc đáo tại {location} với không gian ấm cúng và chủ nhà thân thiện",
                    thumbnail = "https://img.youtube.com/vi/dQw4w9WgXcQ/mqdefault.jpg",
                    channelTitle = "Vietnam Homestay Review",
                    publishedAt = DateTime.Now.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = category
                },
                new {
                    videoId = "M7lc1UVf-VE",
                    title = $"Ăn gì ở homestay {location}? - Món ngon địa phương",
                    description = $"Thưởng thức những món ăn đặc sản tại homestay {location}",
                    thumbnail = "https://img.youtube.com/vi/M7lc1UVf-VE/mqdefault.jpg",
                    channelTitle = "Homestay Food Tour",
                    publishedAt = DateTime.Now.AddDays(-3).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = "food"
                },
                new {
                    videoId = "ScMzIvxBSi4",
                    title = $"Homestay gia đình tại {location} - Phù hợp cho trẻ em",
                    description = $"Homestay thân thiện với trẻ em, không gian an toàn và vui chơi tại {location}",
                    thumbnail = "https://img.youtube.com/vi/ScMzIvxBSi4/mqdefault.jpg",
                    channelTitle = "Family Travel Vietnam",
                    publishedAt = DateTime.Now.AddDays(-5).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = "family"
                },
                new {
                    videoId = "2MUbWLttOKg",
                    title = $"Homestay cao cấp {location} - Luxury Experience",
                    description = $"Trải nghiệm homestay sang trọng với view đẹp và tiện nghi hiện đại tại {location}",
                    thumbnail = "https://img.youtube.com/vi/2MUbWLttOKg/mqdefault.jpg",
                    channelTitle = "Luxury Vietnam Travel",
                    publishedAt = DateTime.Now.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = "luxury"
                },
                new {
                    videoId = "CjZxj5tL_W0",
                    title = $"Văn hóa địa phương tại homestay {location}",
                    description = $"Tìm hiểu văn hóa, phong tục tập quán địa phương thông qua homestay tại {location}",
                    thumbnail = "https://img.youtube.com/vi/CjZxj5tL_W0/mqdefault.jpg",
                    channelTitle = "Vietnam Culture",
                    publishedAt = DateTime.Now.AddDays(-10).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = "culture"
                },
                new {
                    videoId = "qVdPh2cBTN0",
                    title = $"Homestay gần thiên nhiên {location} - Eco Tourism",
                    description = $"Homestay eco-friendly với không gian xanh, gần thiên nhiên tại {location}",
                    thumbnail = "https://img.youtube.com/vi/qVdPh2cBTN0/mqdefault.jpg",
                    channelTitle = "Eco Vietnam Travel",
                    publishedAt = DateTime.Now.AddDays(-12).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    category = "nature"
                }
            };

            // Filter by category if specified
            if (category != "all")
            {
                demoVideos = demoVideos.Where(v => v.GetType().GetProperty("category")?.GetValue(v)?.ToString() == category).ToList();
            }

            return demoVideos.OrderBy(x => Guid.NewGuid()).Take(12).ToList();
        }

        [HttpGet("api/YouTube/homestay-videos/{location}")]
        public async Task<IActionResult> GetHomestayVideos(string location)
        {
            try
            {
                // Decode the location parameter
                location = Uri.UnescapeDataString(location);
                var result = await GetTravelVideosData(location, 6);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, videos = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTravelVideos(string city = "Vietnam", int maxResults = 6)
        {
            var result = await GetTravelVideosData(city, maxResults);
            return Json(result);
        }
    }
}