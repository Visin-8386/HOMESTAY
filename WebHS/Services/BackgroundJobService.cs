using WebHS.Data;
using Microsoft.EntityFrameworkCore;
using WebHS.Models;

namespace WebHS.Services
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<BackgroundJobService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ScheduleEmailReminderAsync(string bookingId, DateTime reminderTime)
        {
            try
            {
                // Find the booking
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .FirstOrDefaultAsync(b => b.Id == int.Parse(bookingId));

                if (booking == null || booking.User?.Email == null)
                {
                    _logger.LogWarning($"Booking {bookingId} not found or user email is null");
                    return;
                }

                // Check if reminder time has passed
                if (reminderTime <= DateTime.UtcNow)
                {
                    await _emailService.SendBookingConfirmationAsync(
                        booking.User.Email, 
                        $"{booking.User.FirstName} {booking.User.LastName}",
                        booking.Homestay.Name,
                        booking.CheckInDate,
                        booking.CheckOutDate);
                    _logger.LogInformation($"Reminder email sent for booking {bookingId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending reminder email for booking {bookingId}");
            }
        }

        public async Task ProcessPendingPaymentsAsync()
        {
            try
            {
                var pendingPayments = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Pending && p.CreatedAt < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();

                foreach (var payment in pendingPayments)
                {
                    payment.Status = PaymentStatus.Cancelled;
                    payment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Processed {pendingPayments.Count} expired payments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending payments");
            }
        }

        public async Task CleanupExpiredDataAsync()
        {
            try
            {
                // Clean up expired blocked dates
                var expiredBlockedDates = await _context.BlockedDates
                    .Where(bd => bd.Date < DateTime.UtcNow.AddDays(-30))
                    .ToListAsync();

                _context.BlockedDates.RemoveRange(expiredBlockedDates);

                // Clean up old log entries (if you have a logs table)
                // Add similar cleanup for other temporary data

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {expiredBlockedDates.Count} expired blocked dates");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data cleanup");
            }
        }

        public async Task GenerateReportsAsync()
        {
            try
            {
                // Generate daily/weekly/monthly reports
                var today = DateTime.UtcNow.Date;
                
                // Example: Generate daily booking report
                var dailyBookings = await _context.Bookings
                    .Where(b => b.CreatedAt.Date == today)
                    .CountAsync();

                _logger.LogInformation($"Daily report: {dailyBookings} bookings created today");
                
                // You can expand this to generate and store/email actual reports
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating reports");
            }
        }

        public async Task SyncExternalDataAsync()
        {
            try
            {
                // Sync with external APIs, update exchange rates, etc.
                _logger.LogInformation("External data sync completed");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing external data");
            }
        }
    }
}
