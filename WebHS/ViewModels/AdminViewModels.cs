using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalHomestays { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalPromotions { get; set; }
        public int ActivePromotions { get; set; }
        public int PendingHomestays { get; set; }
        public List<WebHSUser> RecentUsers { get; set; } = new();
        public List<Booking> RecentBookings { get; set; } = new();
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartData { get; set; } = new();
    }

    public class AdminUserListViewModel
    {
        public List<WebHSUser> Users { get; set; } = new();
        public Dictionary<string, List<string>> UserRoles { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminHomestayListViewModel
    {
        public List<Homestay> Homestays { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedStatus { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminBookingListViewModel
    {
        public List<Booking> Bookings { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string SelectedStatus { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminReportsViewModel
    {
        public int ThisMonthBookings { get; set; }
        public int LastMonthBookings { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public object TopHomestays { get; set; } = new();
        public object TopHosts { get; set; } = new();
    }

    public class AdminReviewViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string HomestayName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminReviewListViewModel
    {
        public List<AdminReviewViewModel> Reviews { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalReviews { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}

