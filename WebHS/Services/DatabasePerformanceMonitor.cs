using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;

namespace WebHS.Services
{
    /// <summary>
    /// Service để monitor và báo cáo performance của database
    /// </summary>
    public class DatabasePerformanceMonitor
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabasePerformanceMonitor> _logger;
        private readonly List<QueryPerformanceInfo> _queryPerformanceLog = new();
        private readonly object _logLock = new();

        public DatabasePerformanceMonitor(
            ApplicationDbContext context,
            ILogger<DatabasePerformanceMonitor> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =================================================================================
        // QUERY PERFORMANCE MONITORING
        // =================================================================================

        /// <summary>
        /// Đo performance của một query
        /// </summary>
        public async Task<T> MeasureQueryPerformance<T>(
            Func<Task<T>> queryFunc,
            string queryName,
            string? additionalInfo = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;
            
            try
            {
                var result = await queryFunc();
                stopwatch.Stop();

                var performanceInfo = new QueryPerformanceInfo
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.Elapsed,
                    StartTime = startTime,
                    Success = true,
                    AdditionalInfo = additionalInfo
                };

                LogQueryPerformance(performanceInfo);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var performanceInfo = new QueryPerformanceInfo
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.Elapsed,
                    StartTime = startTime,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AdditionalInfo = additionalInfo
                };

                LogQueryPerformance(performanceInfo);
                throw;
            }
        }

        /// <summary>
        /// Log thông tin performance
        /// </summary>
        private void LogQueryPerformance(QueryPerformanceInfo info)
        {
            lock (_logLock)
            {
                _queryPerformanceLog.Add(info);
                
                // Giữ chỉ 1000 records gần nhất
                if (_queryPerformanceLog.Count > 1000)
                {
                    _queryPerformanceLog.RemoveAt(0);
                }
            }

            // Log slow queries
            if (info.ExecutionTime.TotalMilliseconds > 1000) // > 1 second
            {
                _logger.LogWarning(
                    "Slow query detected: {QueryName} took {ExecutionTime}ms. Info: {AdditionalInfo}",
                    info.QueryName,
                    info.ExecutionTime.TotalMilliseconds,
                    info.AdditionalInfo);
            }
            else if (info.ExecutionTime.TotalMilliseconds > 500) // > 500ms
            {
                _logger.LogInformation(
                    "Query performance: {QueryName} took {ExecutionTime}ms",
                    info.QueryName,
                    info.ExecutionTime.TotalMilliseconds);
            }

            // Log failed queries
            if (!info.Success)
            {
                _logger.LogError(
                    "Query failed: {QueryName} - {ErrorMessage}",
                    info.QueryName,
                    info.ErrorMessage);
            }
        }

        // =================================================================================
        // PERFORMANCE REPORTS
        // =================================================================================

        /// <summary>
        /// Lấy báo cáo performance
        /// </summary>
        public PerformanceReport GetPerformanceReport(TimeSpan? timeWindow = null)
        {
            timeWindow ??= TimeSpan.FromHours(1);
            var cutoffTime = DateTime.UtcNow - timeWindow;

            List<QueryPerformanceInfo> relevantQueries;
            lock (_logLock)
            {
                relevantQueries = _queryPerformanceLog
                    .Where(q => q.StartTime >= cutoffTime)
                    .ToList();
            }

            if (!relevantQueries.Any())
            {
                return new PerformanceReport
                {
                    TimeWindow = timeWindow.Value,
                    GeneratedAt = DateTime.UtcNow,
                    TotalQueries = 0
                };
            }

            var report = new PerformanceReport
            {
                TimeWindow = timeWindow.Value,
                GeneratedAt = DateTime.UtcNow,
                TotalQueries = relevantQueries.Count,
                SuccessfulQueries = relevantQueries.Count(q => q.Success),
                FailedQueries = relevantQueries.Count(q => !q.Success),
                AverageExecutionTime = TimeSpan.FromMilliseconds(
                    relevantQueries.Average(q => q.ExecutionTime.TotalMilliseconds)),
                MaxExecutionTime = relevantQueries.Max(q => q.ExecutionTime),
                MinExecutionTime = relevantQueries.Min(q => q.ExecutionTime),
                SlowQueries = relevantQueries
                    .Where(q => q.ExecutionTime.TotalMilliseconds > 1000)
                    .OrderByDescending(q => q.ExecutionTime)
                    .Take(10)
                    .ToList(),
                TopQueries = relevantQueries
                    .GroupBy(q => q.QueryName)
                    .Select(g => new QuerySummary
                    {
                        QueryName = g.Key,
                        Count = g.Count(),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(
                            g.Average(q => q.ExecutionTime.TotalMilliseconds)),
                        MaxExecutionTime = g.Max(q => q.ExecutionTime),
                        SuccessRate = (double)g.Count(q => q.Success) / g.Count() * 100
                    })
                    .OrderByDescending(q => q.Count)
                    .Take(10)
                    .ToList()
            };

            return report;
        }

        /// <summary>
        /// Lấy báo cáo database health
        /// </summary>
        public async Task<DatabaseHealthReport> GetDatabaseHealthReport()
        {
            var report = new DatabaseHealthReport
            {
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                // Test connection
                var connectionTestStopwatch = Stopwatch.StartNew();
                await _context.Database.CanConnectAsync();
                connectionTestStopwatch.Stop();
                
                report.CanConnect = true;
                report.ConnectionTime = connectionTestStopwatch.Elapsed;

                // Get basic stats
                var stats = await GetBasicDatabaseStats();
                report.TableStats = stats;

                // Test query performance
                var queryTestStopwatch = Stopwatch.StartNew();
                var sampleHomestays = await _context.Homestays
                    .AsNoTracking()
                    .Take(5)
                    .ToListAsync();
                queryTestStopwatch.Stop();
                
                report.SampleQueryTime = queryTestStopwatch.Elapsed;
                report.IsHealthy = true;

                // Check for performance issues
                if (report.ConnectionTime.TotalMilliseconds > 1000 ||
                    report.SampleQueryTime.TotalMilliseconds > 2000)
                {
                    report.IsHealthy = false;
                    report.Issues.Add("Database response time is slow");
                }

            }
            catch (Exception ex)
            {
                report.CanConnect = false;
                report.IsHealthy = false;
                report.Issues.Add($"Database connection failed: {ex.Message}");
                
                _logger.LogError(ex, "Database health check failed");
            }

            return report;
        }

        /// <summary>
        /// Lấy thống kê cơ bản của database
        /// </summary>
        private async Task<List<TableStatInfo>> GetBasicDatabaseStats()
        {
            try
            {
                var stats = new List<TableStatInfo>();

                // Homestays
                var homestayCount = await _context.Homestays.CountAsync();
                var activeHomestayCount = await _context.Homestays.CountAsync(h => h.IsActive);
                stats.Add(new TableStatInfo
                {
                    TableName = "Homestays",
                    TotalRecords = homestayCount,
                    ActiveRecords = activeHomestayCount
                });

                // Bookings
                var bookingCount = await _context.Bookings.CountAsync();
                var paidBookingCount = await _context.Bookings.CountAsync(b => b.Status == Models.BookingStatus.Paid);
                stats.Add(new TableStatInfo
                {
                    TableName = "Bookings",
                    TotalRecords = bookingCount,
                    ActiveRecords = paidBookingCount
                });

                // Users
                var userCount = await _context.Users.CountAsync();
                var activeUserCount = await _context.Users.CountAsync(u => u.IsActive);
                stats.Add(new TableStatInfo
                {
                    TableName = "Users",
                    TotalRecords = userCount,
                    ActiveRecords = activeUserCount
                });

                // Messages
                var messageCount = await _context.Messages.CountAsync();
                var unreadMessageCount = await _context.Messages.CountAsync(m => !m.IsRead);
                stats.Add(new TableStatInfo
                {
                    TableName = "Messages",
                    TotalRecords = messageCount,
                    ActiveRecords = messageCount - unreadMessageCount
                });

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database stats");
                return new List<TableStatInfo>();
            }
        }

        // =================================================================================
        // RECOMMENDATIONS
        // =================================================================================

        /// <summary>
        /// Đưa ra khuyến nghị để cải thiện performance
        /// </summary>
        public List<string> GetPerformanceRecommendations()
        {
            var recommendations = new List<string>();
            var report = GetPerformanceReport(TimeSpan.FromHours(24));

            if (report.TotalQueries == 0)
            {
                return recommendations;
            }

            // Check average execution time
            if (report.AverageExecutionTime.TotalMilliseconds > 500)
            {
                recommendations.Add("Average query execution time is high. Consider optimizing slow queries or adding indexes.");
            }

            // Check failure rate
            var failureRate = (double)report.FailedQueries / report.TotalQueries * 100;
            if (failureRate > 5)
            {
                recommendations.Add($"Query failure rate is {failureRate:F1}%. Investigate failed queries.");
            }

            // Check for too many slow queries
            if (report.SlowQueries.Count > report.TotalQueries * 0.1)
            {
                recommendations.Add("Too many slow queries detected. Consider database performance tuning.");
            }

            // Check for repetitive slow queries
            var repetitiveSlowQueries = report.TopQueries
                .Where(q => q.AverageExecutionTime.TotalMilliseconds > 1000 && q.Count > 10)
                .ToList();

            if (repetitiveSlowQueries.Any())
            {
                recommendations.Add("Repetitive slow queries detected. These should be prioritized for optimization:");
                foreach (var query in repetitiveSlowQueries)
                {
                    recommendations.Add($"  - {query.QueryName} (avg: {query.AverageExecutionTime.TotalMilliseconds:F0}ms, count: {query.Count})");
                }
            }

            return recommendations;
        }
    }

    // =================================================================================
    // SUPPORTING CLASSES
    // =================================================================================

    public class QueryPerformanceInfo
    {
        public string QueryName { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public DateTime StartTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    public class PerformanceReport
    {
        public TimeSpan TimeWindow { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }
        public int FailedQueries { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public TimeSpan MinExecutionTime { get; set; }
        public List<QueryPerformanceInfo> SlowQueries { get; set; } = new();
        public List<QuerySummary> TopQueries { get; set; } = new();
    }

    public class QuerySummary
    {
        public string QueryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public double SuccessRate { get; set; }
    }

    public class DatabaseHealthReport
    {
        public DateTime GeneratedAt { get; set; }
        public bool CanConnect { get; set; }
        public bool IsHealthy { get; set; }
        public TimeSpan ConnectionTime { get; set; }
        public TimeSpan SampleQueryTime { get; set; }
        public List<TableStatInfo> TableStats { get; set; } = new();
        public List<string> Issues { get; set; } = new();
    }

    public class TableStatInfo
    {
        public string TableName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ActiveRecords { get; set; }
    }
}
