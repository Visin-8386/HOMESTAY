using System.ComponentModel.DataAnnotations;
using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.ViewModels
{
    public class PromotionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WebHSPromotionType Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string StatusText
        {
            get
            {
                if (!IsActive) return "Tạm ngưng";
                if (StartDate > DateTime.UtcNow) return "Sắp diễn ra";                if (EndDate < DateTime.UtcNow) return "Đã hết hạn";
                if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value) return "Đã hết lượt";
                return "Đang hoạt động";
            }
        }

        public string TypeText => Type == WebHSPromotionType.Percentage ? "Phần trăm" : "Số tiền cố định";
    }

    public class PromotionListViewModel
    {
        public List<PromotionViewModel> Promotions { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class CreatePromotionViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khuyến mãi")]
        [StringLength(200, ErrorMessage = "Tên khuyến mãi không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự")]
        [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Mã khuyến mãi chỉ được chứa chữ hoa và số")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại khuyến mãi")]
        public WebHSPromotionType Type { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá trị khuyến mãi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị khuyến mãi phải lớn hơn 0")]
        public decimal Value { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa phải lớn hơn hoặc bằng 0")]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        public bool IsActive { get; set; } = true;
    }

    public class EditPromotionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khuyến mãi")]
        [StringLength(200, ErrorMessage = "Tên khuyến mãi không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự")]
        [RegularExpression("^[A-Z0-9]+$", ErrorMessage = "Mã khuyến mãi chỉ được chứa chữ hoa và số")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại khuyến mãi")]
        public WebHSPromotionType Type { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá trị khuyến mãi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị khuyến mãi phải lớn hơn 0")]
        public decimal Value { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa phải lớn hơn hoặc bằng 0")]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        public int? UsageLimit { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
    }

    public class PromotionStatViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int UsedCount { get; set; }
        public decimal TotalDiscount { get; set; }
    }

    public class PromotionStatisticsViewModel
    {
        public int TotalPromotions { get; set; }
        public int ActivePromotions { get; set; }
        public int TotalUsage { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<PromotionStatViewModel> TopPromotions { get; set; } = new();
    }
}




