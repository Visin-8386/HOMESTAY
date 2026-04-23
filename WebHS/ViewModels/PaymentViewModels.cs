using System.ComponentModel.DataAnnotations;
using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.ViewModels
{
    public class PaymentResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public int BookingId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CheckoutViewModel
    {
        public int BookingId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod SelectedMethod { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public int NumberOfNights { get; set; }
    }
    
    public class PaymentViewModel
    {
        public int BookingId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        // Added properties
        public string Currency { get; set; } = "USD"; // Defaulting to USD, adjust if needed
        public string Description { get; set; } = string.Empty;
    }
    
    public class PaymentMethodViewModel
    {
        public PaymentMethod Method { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }
}

