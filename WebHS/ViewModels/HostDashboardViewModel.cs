using WebHS.Models;

namespace WebHS.ViewModels
{
    public class HostDashboardViewModel
    {
        // Thống kê tổng quan
        public int TotalHomestays { get; set; }
        public int ActiveHomestays { get; set; }
        public int PendingHomestays { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal ThisMonthEarnings { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Thống kê lượt xem
        public int TotalViews { get; set; }
        public int MonthlyViews { get; set; }
        public double BookingConversionRate { get; set; }

        // Top homestays
        public List<HomestayPerformanceViewModel> TopHomestays { get; set; } = new List<HomestayPerformanceViewModel>();

        // Doanh thu theo tháng (12 tháng gần nhất)
        public List<MonthlyRevenueViewModel> MonthlyRevenueData { get; set; } = new List<MonthlyRevenueViewModel>();

        // Booking theo tháng
        public List<MonthlyBookingViewModel> MonthlyBookingData { get; set; } = new List<MonthlyBookingViewModel>();

        // Upcoming bookings
        public List<Booking> UpcomingBookings { get; set; } = new List<Booking>();
        
        // Recent bookings for dashboard display
        public IEnumerable<BookingDetailViewModel> RecentBookings { get; set; } = new List<BookingDetailViewModel>();
        
        // Host's homestays
        public IEnumerable<HomestayCardViewModel> MyHomestays { get; set; } = new List<HomestayCardViewModel>();

        // Calendar data - ngày đã được đặt
        public List<BookedDateViewModel> BookedDates { get; set; } = new List<BookedDateViewModel>();
        
        // Chart data for compatibility with old views
        public List<string> RevenueChartLabels { get; set; } = new List<string>();
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
    }

    public class HomestayPerformanceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public double Rating { get; set; }
        public int Views { get; set; }
        public double ConversionRate { get; set; }
    }

    public class MonthlyRevenueViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class MonthlyBookingViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class BookedDateViewModel
    {
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public BookingStatus Status { get; set; }
    }
}
