using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebHS.Data;
using WebHS.Services;

namespace WebHS.Extensions
{
    /// <summary>
    /// Extension methods để cấu hình database optimization
    /// </summary>
    public static class DatabaseOptimizationExtensions
    {
        /// <summary>
        /// Cấu hình database optimization cho application
        /// </summary>
        public static IServiceCollection AddDatabaseOptimization(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Đăng ký DatabaseOptimizationService
            services.AddScoped<DatabaseOptimizationService>();

            // Cấu hình connection string với optimization
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Timeout cho command execution
                    sqlOptions.CommandTimeout(30);
                    
                    // Enable retry on failure
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });

                // Optimization settings
                if (Environment.GetEnvironmentVariables().Contains("ASPNETCORE_ENVIRONMENT") && 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    // Development settings
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
                else
                {
                    // Production settings
                    options.LogTo(Console.WriteLine, LogLevel.Warning);
                }
            });

            return services;
        }

        /// <summary>
        /// Cấu hình database maintenance tasks
        /// </summary>
        public static IServiceCollection AddDatabaseMaintenance(this IServiceCollection services)
        {
            services.AddHostedService<DatabaseMaintenanceService>();
            return services;
        }
    }

    /// <summary>
    /// Background service cho database maintenance
    /// </summary>
    public class DatabaseMaintenanceService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMaintenanceService> _logger;
        private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(24); // Chạy mỗi 24 giờ

        public DatabaseMaintenanceService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMaintenanceService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbOptimizationService = scope.ServiceProvider.GetRequiredService<DatabaseOptimizationService>();

                    _logger.LogInformation("Starting database maintenance at {Time}", DateTime.UtcNow);

                    // Update statistics
                    await dbOptimizationService.UpdateDatabaseStatistics();

                    // Cleanup old data (mỗi tuần)
                    if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                    {
                        await dbOptimizationService.CleanupOldData();
                    }

                    _logger.LogInformation("Database maintenance completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during database maintenance");
                }

                await Task.Delay(_maintenanceInterval, stoppingToken);
            }
        }
    }
}
