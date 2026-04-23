using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebHS.ViewModels
{
    public class ToggleStatusRequest
    {
        public bool IsActive { get; set; }
    }

    // Admin Homestay ViewModels
    public class AdminCreateHomestayViewModel : CreateHomestayViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn chủ nhà")]
        [Display(Name = "Chủ nhà")]
        public string HostId { get; set; } = string.Empty;

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Đã được duyệt")]
        public bool IsApproved { get; set; } = false;
    }

    public class AdminEditHomestayViewModel : EditHomestayViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn chủ nhà")]
        [Display(Name = "Chủ nhà")]
        public string HostId { get; set; } = string.Empty;

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Đã được duyệt")]
        public bool IsApproved { get; set; } = false;

        [Display(Name = "Quy tắc")]
        public string? Rules { get; set; }
    }

    public class CreateHomestayViewModel
    {
        [Required(ErrorMessage = "Tên homestay là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên homestay")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Thành phố")]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Khu vực")]
        public string State { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Quận/Huyện")]
        public string District { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Phường/Xã")]
        public string Ward { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Quốc gia")]
        public string Country { get; set; } = "Vietnam";

        [Required(ErrorMessage = "Mã bưu điện là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Mã bưu điện")]
        public string ZipCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá mỗi đêm là bắt buộc")]
        [Range(0.01, 99999999, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá mỗi đêm (VNĐ)")]
        public decimal PricePerNight { get; set; }        [Required(ErrorMessage = "Số khách tối đa là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50")]
        [Display(Name = "Số khách tối đa")]
        public int MaxGuests { get; set; } = 1;

        [Required(ErrorMessage = "Số phòng ngủ là bắt buộc")]
        [Range(1, 20, ErrorMessage = "Số phòng ngủ phải từ 1 đến 20")]
        [Display(Name = "Số phòng ngủ")]
        public int Bedrooms { get; set; } = 1;

        [Required(ErrorMessage = "Số phòng tắm là bắt buộc")]
        [Range(1, 20, ErrorMessage = "Số phòng tắm phải từ 1 đến 20")]
        [Display(Name = "Số phòng tắm")]
        public int Bathrooms { get; set; } = 1;

        [Display(Name = "Tiện nghi")]
        public int[]? AmenityIds { get; set; }        [Display(Name = "Hình ảnh")]
        public IEnumerable<IFormFile>? Images { get; set; }

        [StringLength(50)]
        [Display(Name = "YouTube Video ID")]
        public string? YouTubeVideoId { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class EditHomestayViewModel : CreateHomestayViewModel
    {
        public int Id { get; set; }        public IEnumerable<HomestayImage> ExistingImages { get; set; } = new List<HomestayImage>();
        public int[]? ImagesToDelete { get; set; }
    }

    public class HostHomestayListViewModel
    {
        public IEnumerable<Homestay> Homestays { get; set; } = new List<Homestay>();
        public int TotalCount { get; set; }
        public string Status { get; set; } = "all";
    }

    public class HostBookingListViewModel
    {
        public IEnumerable<BookingDetailViewModel> Bookings { get; set; } = new List<BookingDetailViewModel>();
        public string Status { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class HostRevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public Dictionary<string, decimal> RevenueByHomestay { get; set; } = new Dictionary<string, decimal>();
        public List<MonthlyRevenueData> MonthlyRevenue { get; set; } = new List<MonthlyRevenueData>();
    }

    public class MonthlyRevenueData
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }
}

