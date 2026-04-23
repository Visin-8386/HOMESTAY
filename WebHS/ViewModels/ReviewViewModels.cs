using System.ComponentModel.DataAnnotations;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSUser = WebHS.Models.User;

namespace WebHS.ViewModels
{
    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int BookingId { get; set; }
    }

    public class HomestayReviewListViewModel
    {
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public List<ReviewViewModel> Reviews { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }

    public class CreateReviewViewModel
    {
        public int BookingId { get; set; }
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public string? HomestayImage { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(10, ErrorMessage = "Nội dung đánh giá phải có ít nhất 10 ký tự")]
        [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự")]
        public string Comment { get; set; } = string.Empty;
    }

    public class EditReviewViewModel
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public string? HomestayImage { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(10, ErrorMessage = "Nội dung đánh giá phải có ít nhất 10 ký tự")]
        [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1000 ký tự")]
        public string Comment { get; set; } = string.Empty;
    }
}

