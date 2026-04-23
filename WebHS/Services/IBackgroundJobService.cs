namespace WebHS.Services
{
    public interface IBackgroundJobService
    {
        Task ScheduleEmailReminderAsync(string bookingId, DateTime reminderTime);
        Task ProcessPendingPaymentsAsync();
        Task CleanupExpiredDataAsync();
        Task GenerateReportsAsync();
        Task SyncExternalDataAsync();
    }
}
