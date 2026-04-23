namespace WebHS.Services
{
    public class BackgroundJobHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobHostedService> _logger;

        public BackgroundJobHostedService(
            IServiceProvider serviceProvider,
            ILogger<BackgroundJobHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Job Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();

                        // Run different background tasks at different intervals
                        await backgroundJobService.ProcessPendingPaymentsAsync();
                        
                        // Run cleanup daily
                        if (DateTime.UtcNow.Hour == 2) // Run at 2 AM
                        {
                            await backgroundJobService.CleanupExpiredDataAsync();
                            await backgroundJobService.GenerateReportsAsync();
                        }

                        // Run sync every 6 hours
                        if (DateTime.UtcNow.Hour % 6 == 0)
                        {
                            await backgroundJobService.SyncExternalDataAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background job execution");
                }

                // Wait for 1 hour before next execution
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            _logger.LogInformation("Background Job Service stopped");
        }
    }
}
