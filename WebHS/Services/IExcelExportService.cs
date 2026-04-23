using WebHS.Models;
using WebHS.ViewModels;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.Services
{
    public interface IExcelExportService
    {
        byte[] ExportHomestaysToExcel(IEnumerable<Homestay> homestays);
        byte[] ExportBookingsToExcel(IEnumerable<Booking> bookings);
        byte[] ExportUsersToExcel(IEnumerable<UserViewModel> users);
        byte[] ExportPromotionsToExcel(IEnumerable<WebHSPromotion> promotions);
        byte[] ExportMonthlyRevenueToExcel(IEnumerable<MonthlyRevenueData> revenueData);
        byte[] ExportConversationsToExcel(IEnumerable<Conversation> conversations);
        byte[] ExportMessagesToExcel(IEnumerable<Message> messages);
        byte[] ExportHostBookingsToExcel(IEnumerable<BookingDetailViewModel> bookings);
        byte[] ExportHostRevenueToExcel(HostRevenueViewModel revenueData);
    }
}
