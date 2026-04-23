using WebHS.Models;

namespace WebHS.ViewModels
{
    public class HostReviewViewModel
    {
        public int Id { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }
    
    public class AllBookingsViewModel
    {
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();
        public string CurrentStatus { get; set; } = "all";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalBookings { get; set; }
        public int? HomestayFilter { get; set; }
        public List<SelectListItem> Homestays { get; set; } = new List<SelectListItem>();
    }
    
    public class SelectListItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}
