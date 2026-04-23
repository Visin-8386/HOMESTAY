using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Transaction ID is required")]
        [StringLength(100, ErrorMessage = "Transaction ID cannot exceed 100 characters")]
        public string TransactionId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Foreign keys
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookingId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual WebHSUser User { get; set; } = null!;
        
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public string PaymentMethodDisplay => PaymentMethod switch
        {
            PaymentMethod.MoMo => "MoMo",
            PaymentMethod.VNPay => "VNPay",
            PaymentMethod.PayPal => "PayPal", 
            PaymentMethod.Stripe => "Stripe",
            PaymentMethod.BankTransfer => "Bank Transfer",
            PaymentMethod.Free => "Free",
            _ => "Unknown"
        };

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Processing => "Processing",
            PaymentStatus.Completed => "Completed",
            PaymentStatus.Failed => "Failed",
            PaymentStatus.Cancelled => "Cancelled",
            PaymentStatus.Refunded => "Refunded",
            _ => "Unknown"
        };

        [NotMapped]
        public bool IsCompleted => Status == PaymentStatus.Completed;

        [NotMapped]
        public bool CanBeRefunded => Status == PaymentStatus.Completed && PaymentMethod != PaymentMethod.Free;

        [NotMapped]
        public bool IsFailed => Status == PaymentStatus.Failed;

        [NotMapped]
        public TimeSpan? ProcessingTime => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
    }

    public enum PaymentMethod
    {
        MoMo = 0,
        VNPay = 1,
        PayPal = 2,
        Stripe = 3,
        BankTransfer = 4,
        Free = 5 // Phương thức thanh toán miễn phí
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5
    }
}
