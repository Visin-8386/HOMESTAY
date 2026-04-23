using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace WebHS.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, bool> _cacheKeys;

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _cacheKeys = new ConcurrentDictionary<string, bool>();
        }

        public Task<T?> GetAsync<T>(string key)
        {
            _memoryCache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _cacheKeys.TryRemove(evictedKey.ToString()!, out _);
            });

            _memoryCache.Set(key, value, options);
            _cacheKeys.TryAdd(key, true);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            var regex = new Regex(pattern);
            var keysToRemove = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }
    }
}
