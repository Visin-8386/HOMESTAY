using System.Collections.Concurrent;

namespace WebHS.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
        private static readonly ConcurrentDictionary<string, int> _requestCounts = new();
        private static readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
        private const int _maxRequests = 1000;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var now = DateTime.UtcNow;

            // Clean up old entries
            CleanupOldEntries(now);

            // Check if client is rate limited
            if (IsRateLimited(clientId, now))
            {
                _logger.LogWarning($"Rate limit exceeded for client: {clientId}");
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Update request count
            UpdateRequestCount(clientId, now);

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get user ID if authenticated
            var userId = context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user_{userId}";
            }

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            return $"ip_{ipAddress ?? "unknown"}";
        }

        private bool IsRateLimited(string clientId, DateTime now)
        {
            if (!_requestCounts.TryGetValue(clientId, out int count))
            {
                return false;
            }

            if (!_lastRequestTimes.TryGetValue(clientId, out DateTime lastRequestTime))
            {
                return false;
            }

            // If the time window has passed, reset the count
            if (now - lastRequestTime > _timeWindow)
            {
                _requestCounts.TryRemove(clientId, out _);
                _lastRequestTimes.TryRemove(clientId, out _);
                return false;
            }

            return count >= _maxRequests;
        }

        private void UpdateRequestCount(string clientId, DateTime now)
        {
            _requestCounts.AddOrUpdate(clientId, 1, (key, oldValue) => oldValue + 1);
            _lastRequestTimes.AddOrUpdate(clientId, now, (key, oldValue) => now);
        }

        private void CleanupOldEntries(DateTime now)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _lastRequestTimes)
            {
                if (now - kvp.Value > _timeWindow)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _lastRequestTimes.TryRemove(key, out _);
                _requestCounts.TryRemove(key, out _);
            }
        }
    }
}
