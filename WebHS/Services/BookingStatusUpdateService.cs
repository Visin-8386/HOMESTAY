using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public class BookingStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingStatusUpdateService> _logger;

        public BookingStatusUpdateService(
            IServiceProvider serviceProvider,
            ILogger<BookingStatusUpdateService> logger)
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
                    await UpdateBookingStatuses();
                    
                    // Chạy mỗi giờ
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating booking statuses");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
        }

        private async Task UpdateBookingStatuses()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var today = DateTime.Today;
                
                // Tự động cập nhật booking thành Completed khi đã qua ngày checkout
                var bookingsToComplete = await context.Bookings
                    .Where(b => b.Status == BookingStatus.Paid && 
                               b.CheckOutDate.Date < today)
                    .ToListAsync();

                foreach (var booking in bookingsToComplete)
                {
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation($"Auto-completed booking {booking.Id} - checkout date passed");
                }

                // Tự động hủy booking chưa thanh toán sau 24h
                var bookingsToCancel = await context.Bookings
                    .Where(b => b.Status == BookingStatus.Pending && 
                               b.CreatedAt.AddHours(24) < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var booking in bookingsToCancel)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation($"Auto-cancelled booking {booking.Id} - payment timeout");
                }

                if (bookingsToComplete.Any() || bookingsToCancel.Any())
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Updated {bookingsToComplete.Count} completed and {bookingsToCancel.Count} cancelled bookings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update booking statuses");
            }
        }
    }
}
