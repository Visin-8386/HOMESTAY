using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    /// <summary>
    /// Repository với caching để tối ưu hóa database operations
    /// </summary>
    public class CachedHomestayRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedHomestayRepository> _logger;
        private readonly DatabaseOptimizationService _dbOptimization;

        // Cache keys
        private const string POPULAR_HOMESTAYS_KEY = "popular_homestays";
        private const string CITIES_LIST_KEY = "cities_list";
        private const string AMENITIES_LIST_KEY = "amenities_list";
        
        // Cache expiration times
        private readonly TimeSpan _shortCacheTime = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _mediumCacheTime = TimeSpan.FromHours(1);
        private readonly TimeSpan _longCacheTime = TimeSpan.FromHours(6);

        public CachedHomestayRepository(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<CachedHomestayRepository> logger,
            DatabaseOptimizationService dbOptimization)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _dbOptimization = dbOptimization;
        }

        // =================================================================================
        // CACHED QUERIES
        // =================================================================================

        /// <summary>
        /// Lấy danh sách homestay phổ biến với cache
        /// </summary>
        public async Task<List<Homestay>> GetPopularHomestaysAsync(int limit = 10)
        {
            var result = await _cache.GetOrCreateAsync(
                $"{POPULAR_HOMESTAYS_KEY}_{limit}",
                async factory =>
                {
                    factory.SetAbsoluteExpiration(_mediumCacheTime);
                    
                    _logger.LogInformation("Loading popular homestays from database");
                    
                    return await _context.Homestays
                        .AsNoTracking()
                        .Include(h => h.Images.Where(i => i.IsPrimary))
                        .Where(h => h.IsActive && h.IsApproved)
                        .OrderByDescending(h => h.ViewCount)
                        .ThenByDescending(h => h.CreatedAt)
                        .Take(limit)
                        .ToListAsync();
                });
            
            return result ?? new List<Homestay>();
        }

        /// <summary>
        /// Lấy danh sách cities với cache
        /// </summary>
        public async Task<List<string>> GetAvailableCitiesAsync()
        {
            var result = await _cache.GetOrCreateAsync(
                CITIES_LIST_KEY,
                async factory =>
                {
                    factory.SetAbsoluteExpiration(_longCacheTime);
                    
                    _logger.LogInformation("Loading cities list from database");
                    
                    return await _context.Homestays
                        .AsNoTracking()
                        .Where(h => h.IsActive && h.IsApproved)
                        .Select(h => h.City)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync();
                });
            
            return result ?? new List<string>();
        }

        /// <summary>
        /// Lấy danh sách amenities với cache
        /// </summary>
        public async Task<List<Amenity>> GetAvailableAmenitiesAsync()
        {
            var result = await _cache.GetOrCreateAsync(
                AMENITIES_LIST_KEY,
                async factory =>
                {
                    factory.SetAbsoluteExpiration(_longCacheTime);
                    
                    _logger.LogInformation("Loading amenities list from database");
                    
                    return await _context.Amenities
                        .AsNoTracking()
                        .Where(a => a.IsActive)
                        .OrderBy(a => a.Name)
                        .ToListAsync();
                });
            
            return result ?? new List<Amenity>();
        }

        /// <summary>
        /// Tìm kiếm homestay với cache cho kết quả phổ biến
        /// </summary>
        public async Task<List<Homestay>> SearchHomestaysAsync(
            string? city = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? maxGuests = null,
            List<int>? amenityIds = null,
            int skip = 0,
            int take = 20)
        {
            // Tạo cache key từ parameters
            var cacheKey = $"search_{city}_{minPrice}_{maxPrice}_{maxGuests}_{string.Join(",", amenityIds ?? new List<int>())}_{skip}_{take}";
            
            // Chỉ cache cho các tìm kiếm cơ bản và trang đầu tiên
            var shouldCache = skip == 0 && take <= 20 && 
                             (amenityIds == null || amenityIds.Count <= 3);

            if (shouldCache && _cache.TryGetValue(cacheKey, out List<Homestay>? cachedResult))
            {
                _logger.LogInformation("Returning cached search results for key: {CacheKey}", cacheKey);
                return cachedResult!;
            }

            // Query từ database
            var query = _dbOptimization.GetOptimizedHomestaysQuery(city, minPrice, maxPrice, maxGuests);

            // Apply amenities filter if specified
            if (amenityIds != null && amenityIds.Any())
            {
                query = query.Where(h => h.HomestayAmenities
                    .Any(ha => amenityIds.Contains(ha.AmenityId)));
            }

            var results = await query
                .Include(h => h.Images.Where(i => i.IsPrimary))
                .Include(h => h.Host)
                .OrderByDescending(h => h.ViewCount)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            // Cache kết quả nếu phù hợp
            if (shouldCache)
            {
                _cache.Set(cacheKey, results, _shortCacheTime);
                _logger.LogInformation("Cached search results for key: {CacheKey}", cacheKey);
            }

            return results;
        }

        /// <summary>
        /// Lấy homestay detail với cache
        /// </summary>
        public async Task<Homestay?> GetHomestayByIdAsync(int id)
        {
            var cacheKey = $"homestay_detail_{id}";
            
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async factory =>
                {
                    factory.SetAbsoluteExpiration(_mediumCacheTime);
                    
                    _logger.LogInformation("Loading homestay detail from database for ID: {Id}", id);
                    
                    var homestay = await _context.Homestays
                        .AsNoTracking()
                        .Include(h => h.Host)
                        .Include(h => h.Images.OrderBy(i => i.Order))
                        .Include(h => h.HomestayAmenities)
                            .ThenInclude(ha => ha.Amenity)
                        .Include(h => h.PricingRules)
                        .FirstOrDefaultAsync(h => h.Id == id && h.IsActive && h.IsApproved);

                    // Tăng view count (không cache)
                    if (homestay != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "UPDATE Homestays SET ViewCount = ViewCount + 1 WHERE Id = {0}", id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to update view count for homestay {Id}", id);
                            }
                        });
                    }

                    return homestay;
                });
        }

        /// <summary>
        /// Lấy homestays của host với cache
        /// </summary>
        public async Task<List<Homestay>> GetHomestaysByHostAsync(string hostId)
        {
            var cacheKey = $"host_homestays_{hostId}";
            
            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async factory =>
                {
                    factory.SetAbsoluteExpiration(_mediumCacheTime);
                    
                    _logger.LogInformation("Loading host homestays from database for host: {HostId}", hostId);
                    
                    return await _context.Homestays
                        .AsNoTracking()
                        .Include(h => h.Images.Where(i => i.IsPrimary))
                        .Where(h => h.HostId == hostId)
                        .OrderByDescending(h => h.CreatedAt)
                        .ToListAsync();
                });
            
            return result ?? new List<Homestay>();
        }

        // =================================================================================
        // CACHE INVALIDATION
        // =================================================================================

        /// <summary>
        /// Xóa cache khi có thay đổi homestay
        /// </summary>
        public void InvalidateHomestayCache(int? homestayId = null, string? hostId = null)
        {
            try
            {
                // Xóa cache chung
                _cache.Remove(POPULAR_HOMESTAYS_KEY);
                _cache.Remove(CITIES_LIST_KEY);
                
                // Xóa cache cụ thể
                if (homestayId.HasValue)
                {
                    _cache.Remove($"homestay_detail_{homestayId}");
                }
                
                if (!string.IsNullOrEmpty(hostId))
                {
                    _cache.Remove($"host_homestays_{hostId}");
                }

                // Xóa search cache (khó xác định chính xác, nên xóa hết)
                // Trong production, nên sử dụng cache tagging để quản lý tốt hơn
                _logger.LogInformation("Invalidated homestay cache for homestay: {HomestayId}, host: {HostId}", 
                    homestayId, hostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache");
            }
        }

        /// <summary>
        /// Xóa tất cả cache
        /// </summary>
        public void ClearAllCache()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Clear();
                }
                _logger.LogInformation("Cleared all cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        // =================================================================================
        // CACHE WARMING
        // =================================================================================

        /// <summary>
        /// Warm up cache với dữ liệu thường dùng
        /// </summary>
        public async Task WarmUpCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warm-up");

                // Warm up popular data
                await GetPopularHomestaysAsync();
                await GetAvailableCitiesAsync();
                await GetAvailableAmenitiesAsync();

                // Warm up common searches
                await SearchHomestaysAsync(take: 10); // General search
                
                // Lấy các thành phố phổ biến và warm up search cho từng thành phố
                var cities = await GetAvailableCitiesAsync();
                var popularCities = cities.Take(5);
                
                foreach (var city in popularCities)
                {
                    await SearchHomestaysAsync(city: city, take: 10);
                }

                _logger.LogInformation("Cache warm-up completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warm-up");
            }
        }
    }
}
