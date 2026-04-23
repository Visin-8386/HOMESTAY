using WebHS.Models;

namespace WebHS.ViewModels
{
    public class UserProfileViewModel
    {
        public User User { get; set; } = null!;
        public int TotalBookings { get; set; }
        public int TotalReviews { get; set; }
        public int TotalHomestays { get; set; }
    }
}
