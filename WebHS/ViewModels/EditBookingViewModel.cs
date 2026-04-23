using System.ComponentModel.DataAnnotations;
using WebHS.Models;

namespace WebHS.ViewModels
{
    public class EditBookingViewModel
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int HomestayId { get; set; }

        [Required]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Required]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50")]
        [Display(Name = "Số khách")]
        public int NumberOfGuests { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giảm giá")]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền cuối cùng phải lớn hơn 0")]
        [Display(Name = "Số tiền cuối cùng")]
        public decimal FinalAmount { get; set; }

        [Display(Name = "Trạng thái")]
        public BookingStatus Status { get; set; }

        [StringLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        // Display properties (read-only)
        public string HomestayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
