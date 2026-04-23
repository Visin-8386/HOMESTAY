using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHS.Services;
using WebHS.Attributes;

namespace WebHS.Controllers
{
    /// <summary>
    /// Controller để quản lý và monitor database performance
    /// </summary>
    [Authorize]
    [CustomAuthorize(Roles = "Admin")]
    public class DatabaseAdminController : Controller
    {
        private readonly DatabaseOptimizationService _dbOptimization;
        private readonly DatabasePerformanceMonitor _performanceMonitor;
        private readonly CachedHomestayRepository _cachedHomestayRepo;
        private readonly ILogger<DatabaseAdminController> _logger;

        public DatabaseAdminController(
            DatabaseOptimizationService dbOptimization,
            DatabasePerformanceMonitor performanceMonitor,
            CachedHomestayRepository cachedHomestayRepo,
            ILogger<DatabaseAdminController> logger)
        {
            _dbOptimization = dbOptimization;
            _performanceMonitor = performanceMonitor;
            _cachedHomestayRepo = cachedHomestayRepo;
            _logger = logger;
        }

        // =================================================================================
        // DASHBOARD & OVERVIEW
        // =================================================================================

        /// <summary>
        /// Dashboard tổng quan về database performance
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var healthReport = await _performanceMonitor.GetDatabaseHealthReport();
                var performanceReport = _performanceMonitor.GetPerformanceReport(TimeSpan.FromHours(1));
                var recommendations = _performanceMonitor.GetPerformanceRecommendations();

                var viewModel = new DatabaseAdminViewModel
                {
                    HealthReport = healthReport,
                    PerformanceReport = performanceReport,
                    Recommendations = recommendations
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading database admin dashboard");
                TempData["Error"] = "Có lỗi khi tải dashboard database.";
                return RedirectToAction("Index", "Home");
            }
        }

        // =================================================================================
        // PERFORMANCE MONITORING
        // =================================================================================

        /// <summary>
        /// Xem báo cáo performance chi tiết
        /// </summary>
        public IActionResult PerformanceReport(int hours = 24)
        {
            try
            {
                var timeWindow = TimeSpan.FromHours(Math.Max(1, Math.Min(168, hours))); // 1 hour to 1 week
                var report = _performanceMonitor.GetPerformanceReport(timeWindow);
                
                ViewBag.TimeWindow = hours;
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading performance report");
                TempData["Error"] = "Có lỗi khi tải báo cáo performance.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Lấy health check data cho API
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var healthReport = await _performanceMonitor.GetDatabaseHealthReport();
                return Json(new
                {
                    isHealthy = healthReport.IsHealthy,
                    canConnect = healthReport.CanConnect,
                    connectionTime = healthReport.ConnectionTime.TotalMilliseconds,
                    sampleQueryTime = healthReport.SampleQueryTime.TotalMilliseconds,
                    issues = healthReport.Issues,
                    tableStats = healthReport.TableStats,
                    generatedAt = healthReport.GeneratedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                return Json(new { isHealthy = false, error = ex.Message });
            }
        }

        // =================================================================================
        // DATABASE MAINTENANCE
        // =================================================================================

        /// <summary>
        /// Cập nhật database statistics
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateStatistics()
        {
            try
            {
                await _dbOptimization.UpdateDatabaseStatistics();
                TempData["Success"] = "Đã cập nhật database statistics thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating database statistics");
                TempData["Error"] = "Có lỗi khi cập nhật database statistics.";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Reindex database tables
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ReindexTables()
        {
            try
            {
                await _dbOptimization.ReindexTables();
                TempData["Success"] = "Đã reindex database tables thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reindexing database tables");
                TempData["Error"] = "Có lỗi khi reindex database tables.";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Cleanup old data
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CleanupOldData(int daysToKeep = 365)
        {
            try
            {
                if (daysToKeep < 30)
                {
                    TempData["Error"] = "Số ngày phải lớn hơn 30.";
                    return RedirectToAction("Index");
                }

                await _dbOptimization.CleanupOldData(daysToKeep);
                TempData["Success"] = $"Đã cleanup dữ liệu cũ hơn {daysToKeep} ngày.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old data");
                TempData["Error"] = "Có lỗi khi cleanup dữ liệu cũ.";
            }

            return RedirectToAction("Index");
        }

        // =================================================================================
        // CACHE MANAGEMENT
        // =================================================================================

        /// <summary>
        /// Cache management page
        /// </summary>
        public IActionResult CacheManagement()
        {
            return View();
        }

        /// <summary>
        /// Warm up cache
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> WarmUpCache()
        {
            try
            {
                await _cachedHomestayRepo.WarmUpCacheAsync();
                TempData["Success"] = "Đã warm up cache thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up cache");
                TempData["Error"] = "Có lỗi khi warm up cache.";
            }

            return RedirectToAction("CacheManagement");
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        [HttpPost]
        public IActionResult ClearAllCache()
        {
            try
            {
                _cachedHomestayRepo.ClearAllCache();
                TempData["Success"] = "Đã xóa tất cả cache thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                TempData["Error"] = "Có lỗi khi xóa cache.";
            }

            return RedirectToAction("CacheManagement");
        }

        /// <summary>
        /// Invalidate homestay cache
        /// </summary>
        [HttpPost]
        public IActionResult InvalidateHomestayCache(int? homestayId = null, string? hostId = null)
        {
            try
            {
                _cachedHomestayRepo.InvalidateHomestayCache(homestayId, hostId);
                TempData["Success"] = "Đã invalidate cache thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache");
                TempData["Error"] = "Có lỗi khi invalidate cache.";
            }

            return RedirectToAction("CacheManagement");
        }

        // =================================================================================
        // DATABASE INFORMATION
        // =================================================================================

        /// <summary>
        /// Xem thông tin chi tiết về database
        /// </summary>
        public async Task<IActionResult> DatabaseInfo()
        {
            try
            {
                var performanceInfo = await _dbOptimization.GetDatabasePerformanceInfo();
                return View(performanceInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading database info");
                TempData["Error"] = "Có lỗi khi tải thông tin database.";
                return RedirectToAction("Index");
            }
        }

        // =================================================================================
        // API ENDPOINTS
        // =================================================================================

        /// <summary>
        /// API để lấy performance metrics
        /// </summary>
        [HttpGet]
        public IActionResult GetPerformanceMetrics(int hours = 1)
        {
            try
            {
                var timeWindow = TimeSpan.FromHours(Math.Max(1, Math.Min(24, hours)));
                var report = _performanceMonitor.GetPerformanceReport(timeWindow);

                return Json(new
                {
                    totalQueries = report.TotalQueries,
                    successfulQueries = report.SuccessfulQueries,
                    failedQueries = report.FailedQueries,
                    averageExecutionTime = report.AverageExecutionTime.TotalMilliseconds,
                    maxExecutionTime = report.MaxExecutionTime.TotalMilliseconds,
                    slowQueriesCount = report.SlowQueries.Count,
                    topQueries = report.TopQueries.Take(5).Select(q => new
                    {
                        queryName = q.QueryName,
                        count = q.Count,
                        averageTime = q.AverageExecutionTime.TotalMilliseconds,
                        successRate = q.SuccessRate
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return Json(new { error = ex.Message });
            }
        }
    }

    // =================================================================================
    // VIEW MODELS
    // =================================================================================

    public class DatabaseAdminViewModel
    {
        public DatabaseHealthReport HealthReport { get; set; } = new();
        public PerformanceReport PerformanceReport { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
