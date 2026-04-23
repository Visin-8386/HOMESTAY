using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using System.ComponentModel.DataAnnotations;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.ViewModels
{
    public class BookingViewModel : IValidatableObject
    {
        public int HomestayId { get; set; }

        [Required(ErrorMessage = "Ngày nhận phòng là bắt buộc")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Ngày trả phòng là bắt buộc")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Số khách là bắt buộc")]
        [Range(1, 20, ErrorMessage = "Số khách phải từ 1 đến 20")]
        [Display(Name = "Số khách")]
        public int NumberOfGuests { get; set; } = 1;        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string? PromotionCode { get; set; }

        // Calculated fields
        public decimal PricePerNight { get; set; }
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string HomestayName { get; set; } = string.Empty;

        // ADDED: Enhanced validation logic
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate date range
            if (CheckOutDate <= CheckInDate)
            {
                results.Add(new ValidationResult(
                    "Ngày trả phòng phải sau ngày nhận phòng",
                    new[] { nameof(CheckOutDate) }));
            }

            // Validate minimum stay
            var numberOfNights = (CheckOutDate - CheckInDate).Days;
            if (numberOfNights < 1)
            {
                results.Add(new ValidationResult(
                    "Thời gian lưu trú tối thiểu là 1 đêm",
                    new[] { nameof(CheckOutDate) }));
            }

            // Validate not in past
            if (CheckInDate < DateTime.Today)
            {
                results.Add(new ValidationResult(
                    "Ngày nhận phòng không thể trong quá khứ",
                    new[] { nameof(CheckInDate) }));
            }

            return results;
        }
    }

    public class BookingListViewModel
    {
        public IEnumerable<BookingDetailViewModel> Bookings { get; set; } = new List<BookingDetailViewModel>();
        public string Status { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class BookingDetailViewModel
    {
        public int Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal FinalAmount { get; set; }
        public BookingStatus Status { get; set; }
        public WebHSUser User { get; set; } = new WebHSUser();
        public Homestay Homestay { get; set; } = new Homestay();
        public Booking Booking { get; set; } = new Booking();
        public string HomestayName { get; set; } = string.Empty;
        public string PrimaryImage { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public bool CanReview { get; set; }
        public bool CanCancel { get; set; }

        // Added properties
        public decimal DiscountAmount { get; set; }
        public string UserName { get; set; } = string.Empty; 
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public string HomestayLocation { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Review fields are now part of the Booking entity
        public string? HomestayImage { get; set; }
    }

    // ADDED: Booking address data transfer object
    public class BookingAddressData
    {
        public string? HouseNumber { get; set; }
        public string? StreetName { get; set; }
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? FullAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}

