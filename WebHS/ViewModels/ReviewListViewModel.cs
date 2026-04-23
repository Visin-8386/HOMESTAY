using WebHS.Models;

namespace WebHS.ViewModels
{
    public class ReviewListViewModel
    {
        public List<ReviewItemViewModel> Reviews { get; set; } = new List<ReviewItemViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public string SearchTerm { get; set; } = "";
        public int? FilterRating { get; set; }
        public string SortBy { get; set; } = "newest"; // newest, oldest, highest, lowest
    }

    public class ReviewItemViewModel
    {
        public int BookingId { get; set; }
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = "";
        public string HomestayLocation { get; set; } = "";
        public string HomestayImage { get; set; } = "";
        public string UserName { get; set; } = "";
        public string UserInitials { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string FormattedCreatedAt { get; set; } = "";
        public string RatingStars { get; set; } = "";
        public bool IsRecentReview { get; set; }
        public string HostName { get; set; } = "";
    }
}
