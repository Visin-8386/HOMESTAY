using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using WebHS.Data;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Services;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IPayPalService _payPalService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ICurrencyService _currencyService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IBookingService _bookingService;        public PaymentController(
            ApplicationDbContext context,
            UserManager<WebHSUser> userManager,
            IConfiguration configuration,
            IPayPalService payPalService,
            IEmailService emailService,
            ILogger<PaymentController> logger,
            ICurrencyService currencyService,
            IWebHostEnvironment hostEnvironment,
            IBookingService bookingService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _payPalService = payPalService;
            _emailService = emailService;
            _logger = logger;
            _currencyService = currencyService;
            _hostEnvironment = hostEnvironment;
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Đặt phòng không hợp lệ hoặc không tồn tại";
                return RedirectToAction("Index", "Booking");
            }            // Check if booking is already completed or cancelled
            if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            {
                TempData["Error"] = "Đặt phòng này đã được xử lý hoặc đã bị hủy";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }

            // If booking is already paid, redirect to booking details
            if (booking.Status == BookingStatus.Paid)
            {
                TempData["Message"] = "Đặt phòng này đã được thanh toán thành công";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }

            // Check if the booking is still valid (dates)
            if (booking.CheckInDate < DateTime.Today)
            {
                TempData["Error"] = "Đặt phòng này đã hết hạn do đã qua ngày nhận phòng";
                return RedirectToAction("Index", "Booking");
            }            // Prepare payment view model
            var viewModel = new PaymentViewModel
            {
                BookingId = booking.Id,
                HomestayName = booking.Homestay.Name,
                Amount = booking.FinalAmount,
                Description = $"Thanh toán đặt phòng {booking.Homestay.Name} từ {booking.CheckInDate:dd/MM/yyyy} đến {booking.CheckOutDate:dd/MM/yyyy} cho {booking.NumberOfGuests} khách"
            };
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
                
            if (booking == null)
            {
                return Json(new { success = false, message = "Đặt phòng không hợp lệ" });
            }

            try
            {
                // Parse payment method string to enum
                if (!Enum.TryParse<PaymentMethod>(paymentMethod, true, out var paymentMethodEnum))
                {
                    return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                }

                // SECURITY: Block free payment in production environment
                var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                if (paymentMethodEnum == PaymentMethod.Free && environment?.IsDevelopment() != true)
                {
                    return Json(new { success = false, message = "Phương thức thanh toán miễn phí chỉ khả dụng trong môi trường development" });
                }

                // Check if there's already a pending payment for this booking
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == PaymentStatus.Pending);

                Payment payment;
                if (existingPayment != null)
                {
                    // Update existing payment if payment method has changed
                    if (existingPayment.PaymentMethod != paymentMethodEnum)
                    {
                        existingPayment.PaymentMethod = paymentMethodEnum;
                        existingPayment.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    payment = existingPayment;
                }                else
                {
                    // Create a new payment with a unique TransactionId
                    var transactionId = paymentMethodEnum == PaymentMethod.Free 
                        ? $"FREE_{Guid.NewGuid():N}_{DateTimeOffset.Now.ToUnixTimeSeconds()}"
                        : $"PENDING_{Guid.NewGuid():N}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";

                    payment = new Payment
                    {
                        BookingId = bookingId,
                        UserId = userId ?? string.Empty, // Ensure non-null value
                        Amount = booking.FinalAmount,
                        PaymentMethod = paymentMethodEnum,
                        Status = PaymentStatus.Pending,
                        TransactionId = transactionId, // Set TransactionId before saving
                        CreatedAt = DateTime.Now
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                }                // Generate payment URL based on method
                string paymentUrl = paymentMethod.ToLower() switch
                {
                    "momo" => await GenerateMoMoPaymentUrl(payment, booking),
                    "vnpay" => await GenerateVNPayPaymentUrl(payment, booking),
                    "paypal" => await GeneratePayPalPaymentUrl(payment, booking),
                    "free" => await ProcessFreePayment(payment, booking),                    _ => throw new ArgumentException("Phương thức thanh toán không được hỗ trợ")
                };

                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message });
            }
        }        [HttpGet]        public async Task<IActionResult> PaymentReturn(string paymentMethod, string paymentId, string status)
        {
            _logger.LogInformation("PaymentReturn called - Method: {PaymentMethod}, PaymentId: {PaymentId}, Status: {Status}", paymentMethod, paymentId, status);
            
            // PayPal returns different parameters than other payment methods
            if (paymentMethod == "paypal")
            {
                var token = Request.Query["token"].ToString();
                var payerID = Request.Query["PayerID"].ToString();
                
                _logger.LogInformation("PayPal return - Token: {Token}, PayerID: {PayerID}", token, payerID);
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("PayPal return - Missing token");
                    TempData["Error"] = "Thông tin thanh toán PayPal không hợp lệ";
                    return RedirectToAction("Index", "Booking");
                }
                
                // Find payment by PayPal token (stored as TransactionId)
                var payment = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Homestay)
                    .Include(p => p.Booking.User)
                    .FirstOrDefaultAsync(p => p.TransactionId == token);
                
                if (payment == null)
                {
                    _logger.LogWarning("PayPal return - Payment not found for token: {Token}", token);
                    TempData["Error"] = "Không tìm thấy giao dịch thanh toán PayPal";
                    return RedirectToAction("Index", "Booking");
                }
                
                _logger.LogInformation("PayPal return - Found payment {PaymentId} for token {Token}", payment.Id, token);
                
                var isSuccess = await ValidatePaymentReturn(paymentMethod, Request.Query);
                _logger.LogInformation("PayPal validation result: {IsSuccess}", isSuccess);
                var viewModel = new PaymentResultViewModel
                {
                    TransactionId = payment.TransactionId,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    BookingId = payment.BookingId,
                    HomestayName = payment.Booking?.Homestay?.Name ?? "Unknown",
                    PaymentDate = DateTime.Now,
                    IsSuccess = isSuccess
                };
                
                if (isSuccess)
                {
                    // Only process if the payment is still pending
                    if (payment.Status == PaymentStatus.Pending)
                    {
                        payment.Status = PaymentStatus.Completed;
                        payment.CompletedAt = DateTime.Now;                        // Update booking status
                        if (payment.Booking != null)
                        {
                            payment.Booking.Status = BookingStatus.Paid;
                            
                            // Send confirmation email
                            try
                            {
                                var user = payment.Booking.User;
                                if (user?.Email != null)
                                {
                                    await _emailService.SendDetailedBookingConfirmationAsync(user.Email, payment.Booking);
                                    _logger.LogInformation($"Payment confirmation email sent to {user.Email} for booking #{payment.Booking.Id}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send payment confirmation email for booking #{payment.Booking.Id}");
                            }
                        }

                        await _context.SaveChangesAsync();
                        
                        // Create blocked dates for the paid booking
                        try
                        {
                            await _bookingService.CreateBlockedDatesForPaidBookingAsync(payment.BookingId);
                            _logger.LogInformation("Created blocked dates for paid booking {BookingId}", payment.BookingId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create blocked dates for booking {BookingId}", payment.BookingId);
                            // Don't fail the payment process if blocked dates creation fails
                        }
                    }
                    
                    viewModel.Message = "Thanh toán PayPal thành công!";
                    TempData["Success"] = viewModel.Message;
                }
                else
                {
                    viewModel.Message = "Thanh toán PayPal không thành công hoặc đã bị hủy";
                    TempData["Error"] = viewModel.Message;
                }
                
                return View("Result", viewModel);
            }
            
            // Handle other payment methods (existing logic)
            var paymentForOthers = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Homestay)
                .Include(p => p.Booking.User)
                .FirstOrDefaultAsync(p => p.TransactionId == paymentId);

            if (paymentForOthers == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch thanh toán";
                return RedirectToAction("Index", "Booking");
            }

            var isSuccessOthers = await ValidatePaymentReturn(paymentMethod, Request.Query);
            var viewModelOthers = new PaymentResultViewModel
            {
                TransactionId = paymentForOthers.TransactionId,
                Amount = paymentForOthers.Amount,
                PaymentMethod = paymentForOthers.PaymentMethod,
                BookingId = paymentForOthers.BookingId,
                HomestayName = paymentForOthers.Booking?.Homestay?.Name ?? "Unknown",
                PaymentDate = DateTime.Now,
                IsSuccess = isSuccessOthers
            };

            if (isSuccessOthers)
            {
                // Only process if the payment is still pending
                if (paymentForOthers.Status == PaymentStatus.Pending)
                {
                    paymentForOthers.Status = PaymentStatus.Completed;
                    paymentForOthers.CompletedAt = DateTime.Now;

                    // Update booking status
                    if (paymentForOthers.Booking != null)
                    {
                        paymentForOthers.Booking.Status = BookingStatus.Paid;
                        
                        // Send confirmation email
                        try
                        {
                            var user = paymentForOthers.Booking.User;
                            if (user?.Email != null)
                            {
                                await _emailService.SendDetailedBookingConfirmationAsync(user.Email, paymentForOthers.Booking);
                                _logger.LogInformation($"Payment confirmation email sent to {user.Email} for booking #{paymentForOthers.Booking.Id}");
                            }
                        }                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send payment confirmation email for booking #{paymentForOthers.Booking.Id}");
                        }
                    }

                    await _context.SaveChangesAsync();
                    
                    // Create blocked dates for the paid booking
                    try
                    {
                        await _bookingService.CreateBlockedDatesForPaidBookingAsync(paymentForOthers.BookingId);
                        _logger.LogInformation("Created blocked dates for paid booking {BookingId}", paymentForOthers.BookingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create blocked dates for booking {BookingId}", paymentForOthers.BookingId);
                        // Don't fail the payment process if blocked dates creation fails
                    }
                }
                
                viewModelOthers.Message = "Thanh toán thành công! Đặt phòng của bạn đã được xác nhận.";
                TempData["Success"] = viewModelOthers.Message;
            }
            else
            {
                // Update payment status to failed if still pending
                if (paymentForOthers.Status == PaymentStatus.Pending)
                {
                    paymentForOthers.Status = PaymentStatus.Failed;
                    paymentForOthers.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                
                viewModelOthers.Message = "Thanh toán thất bại. Vui lòng thử lại.";
                TempData["Error"] = viewModelOthers.Message;
            }            return View("Result", viewModelOthers);
        }

        [HttpPost]
        public IActionResult PaymentNotify(string paymentMethod)
        {
            try
            {
                var isValid = ValidatePaymentNotification(paymentMethod, Request);

                if (isValid)
                {
                    // Process the notification
                    // This would typically involve updating payment status
                    // and sending confirmation emails
                    
                    return Ok("00"); // Success response
                }
                else
                {
                    return BadRequest("Invalid notification");
                }
            }
            catch (Exception)
            {
                // Log error
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Result(string transactionId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Homestay)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return RedirectToAction("Index", "Booking");
            }

            var viewModel = new PaymentResultViewModel
            {
                IsSuccess = payment.Status == PaymentStatus.Completed,
                TransactionId = payment.TransactionId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                BookingId = payment.BookingId,
                HomestayName = payment.Booking?.Homestay?.Name ?? "Unknown",
                PaymentDate = payment.CompletedAt ?? payment.CreatedAt,
                Message = payment.Status == PaymentStatus.Completed 
                    ? "Thanh toán thành công!" 
                    : "Thanh toán thất bại!"
            };

            return View(viewModel);
        }

        private async Task<string> GenerateMoMoPaymentUrl(Payment payment, Booking booking)
        {
            var endpoint = _configuration["MoMo:Endpoint"];
            var partnerCode = _configuration["MoMo:PartnerCode"];
            var accessKey = _configuration["MoMo:AccessKey"];
            var secretKey = _configuration["MoMo:SecretKey"];
            
            var orderId = $"ORDER_{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            var orderInfo = $"Thanh toán đặt phòng #{booking.Id}";
            var redirectUrl = Url.Action("PaymentReturn", "Payment", new { paymentMethod = "momo" }, Request.Scheme);
            var ipnUrl = Url.Action("PaymentNotify", "Payment", new { paymentMethod = "momo" }, Request.Scheme);
            var amount = payment.Amount.ToString("F0");
            var requestId = orderId;
            var requestType = "captureWallet";
            var extraData = "";

            // Create signature
            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            var signature = HmacSHA256(rawSignature, secretKey ?? "");

            var requestData = new
            {
                partnerCode = partnerCode,
                partnerName = "WebHS",
                storeId = "MomoTestStore",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };            // In a real implementation, you would make an HTTP POST request to MoMo API
            // and return the payment URL from the response
            // Update the existing TransactionId with MoMo's order ID
            payment.TransactionId = orderId;
            await _context.SaveChangesAsync();

            return $"{endpoint}?orderId={orderId}&amount={amount}";
        }

        private async Task<string> GenerateVNPayPaymentUrl(Payment payment, Booking booking)
        {
            var vnp_TmnCode = _configuration["VNPay:TmnCode"];
            var vnp_HashSecret = _configuration["VNPay:HashSecret"];
            var vnp_Url = _configuration["VNPay:Url"];
            var vnp_ReturnUrl = Url.Action("PaymentReturn", "Payment", new { paymentMethod = "vnpay" }, Request.Scheme);

            var vnp_TxnRef = $"{payment.Id}_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
            var vnp_OrderInfo = $"Thanh toan dat phong #{booking.Id}";
            var vnp_OrderType = "other";
            var vnp_Amount = (payment.Amount * 100).ToString("F0"); // VNPay requires amount in VND cents
            var vnp_Locale = "vn";
            var vnp_BankCode = "";
            var vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var vnp_CurrCode = "VND";
            var vnp_IpAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var vnp_Version = "2.1.0";
            var vnp_Command = "pay";

            var vnpData = new SortedDictionary<string, string>
            {
                {"vnp_Version", vnp_Version},
                {"vnp_Command", vnp_Command},
                {"vnp_TmnCode", vnp_TmnCode ?? ""},
                {"vnp_Amount", vnp_Amount},
                {"vnp_CreateDate", vnp_CreateDate},
                {"vnp_CurrCode", vnp_CurrCode},
                {"vnp_IpAddr", vnp_IpAddr},
                {"vnp_Locale", vnp_Locale},
                {"vnp_OrderInfo", vnp_OrderInfo},
                {"vnp_OrderType", vnp_OrderType},
                {"vnp_ReturnUrl", vnp_ReturnUrl ?? ""},
                {"vnp_TxnRef", vnp_TxnRef}
            };

            if (!string.IsNullOrEmpty(vnp_BankCode))
            {
                vnpData.Add("vnp_BankCode", vnp_BankCode);
            }

            var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var vnp_SecureHash = HmacSHA512(query, vnp_HashSecret ?? "");            
            payment.TransactionId = vnp_TxnRef;
            await _context.SaveChangesAsync();

            return $"{vnp_Url}?{query}&vnp_SecureHash={vnp_SecureHash}";
        }

        private async Task<string> GeneratePayPalPaymentUrl(Payment payment, Booking booking)
        {
            try
            {
                var approvalUrl = await _payPalService.CreatePaymentAsync(payment, booking);
                await _context.SaveChangesAsync();
                return approvalUrl;
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception($"Failed to create PayPal payment: {ex.Message}", ex);
            }
        }        // Handle free payment method
        private async Task<string> ProcessFreePayment(Payment payment, Booking booking)
        {
            // TransactionId is already set during payment creation, no need to set it again
            
            // DO NOT automatically complete the payment - keep it pending
            // Let the user confirm payment on a separate page
            payment.Status = PaymentStatus.Pending;
            payment.UpdatedAt = DateTime.Now;
            
            // Keep booking status as pending until user confirms
            if (booking != null)
            {
                booking.Status = BookingStatus.Pending;
            }
            
            await _context.SaveChangesAsync();
            
            // Redirect to a confirmation page instead of completing immediately
            return Url.Action("ConfirmFreePayment", "Payment", new { transactionId = payment.TransactionId }, Request.Scheme) ?? "/";
        }

        // Action for confirming free payment
        [HttpGet]
        public async Task<IActionResult> ConfirmFreePayment(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return BadRequest("TransactionId is required");
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Homestay)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return NotFound("Payment not found");
            }

            // Chỉ cho phép xác nhận payment trong môi trường development
            if (!_hostEnvironment.IsDevelopment())
            {
                return Forbid("Free payment is only available in development environment");
            }

            // Chỉ cho phép xác nhận nếu payment đang pending
            if (payment.Status != PaymentStatus.Pending)
            {
                return BadRequest("Payment is not in pending status");
            }

            // Pass payment and booking information to view
            ViewBag.Payment = payment;
            ViewBag.Booking = payment.Booking;
            
            return View(payment);
        }

        // Action to process free payment confirmation
        [HttpPost]
        public async Task<IActionResult> ConfirmFreePayment(string transactionId, bool confirmed)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return BadRequest("TransactionId is required");
            }

            if (!confirmed)
            {
                return RedirectToAction("Details", "Booking", new { id = transactionId });
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return NotFound("Payment not found");
            }

            // Chỉ cho phép xác nhận payment trong môi trường development
            if (!_hostEnvironment.IsDevelopment())
            {
                return Forbid("Free payment is only available in development environment");
            }

            // Chỉ cho phép xác nhận nếu payment đang pending
            if (payment.Status != PaymentStatus.Pending)
            {
                return BadRequest("Payment is not in pending status");
            }

            try
            {
                // NOW we can mark payment as completed
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;

                // Update booking status to paid
                if (payment.Booking != null)
                {
                    payment.Booking.Status = BookingStatus.Paid;

                    // Send confirmation email for free booking
                    try
                    {
                        var user = payment.Booking.User;
                        if (user?.Email != null)
                        {
                            await _emailService.SendDetailedBookingConfirmationAsync(user.Email, payment.Booking);
                            _logger.LogInformation($"Free booking confirmation email sent to {user.Email} for booking #{payment.Booking.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send free booking confirmation email for booking #{payment.Booking.Id}");
                    }
                }                await _context.SaveChangesAsync();
                
                // Create blocked dates for the paid booking
                try
                {
                    await _bookingService.CreateBlockedDatesForPaidBookingAsync(payment.BookingId);
                    _logger.LogInformation("Created blocked dates for paid booking {BookingId}", payment.BookingId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create blocked dates for booking {BookingId}", payment.BookingId);
                    // Don't fail the payment process if blocked dates creation fails
                }
                
                _logger.LogInformation($"Free payment confirmed for TransactionId: {transactionId}");

                // Redirect to success page
                return RedirectToAction("Result", "Payment", new { transactionId = transactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming free payment for TransactionId: {transactionId}");
                ModelState.AddModelError("", "An error occurred while processing your payment confirmation");
                
                // Return to confirmation page with error
                var paymentForView = await _context.Payments
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.Homestay)
                    .Include(p => p.Booking)
                        .ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
                
                ViewBag.Payment = paymentForView;
                ViewBag.Booking = paymentForView?.Booking;
                
                return View(paymentForView);
            }
        }

        // HMAC-SHA256 Hash generator for MoMo payment
        private string HmacSHA256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private string HmacSHA512(string inputData, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task<bool> ValidatePaymentReturn(string paymentMethod, IQueryCollection queryParams)
        {
            try
            {
                return paymentMethod.ToLower() switch
                {
                    "momo" => ValidateMoMoReturn(queryParams),
                    "vnpay" => ValidateVNPayReturn(queryParams),
                    "paypal" => await ValidatePayPalReturn(queryParams),
                    "free" => true, // Free payments are always valid
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ValidatePaymentNotification(string paymentMethod, HttpRequest request)
        {
            try
            {
                return paymentMethod.ToLower() switch
                {
                    "momo" => ValidateMoMoNotification(request),
                    "vnpay" => ValidateVNPayNotification(request),
                    "paypal" => ValidatePayPalNotification(request),
                    "free" => true, // Free payments don't need notifications
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ValidateMoMoReturn(IQueryCollection queryParams)
        {
            // MoMo validation logic
            var partnerCode = queryParams["partnerCode"].ToString();
            var orderId = queryParams["orderId"].ToString();
            var requestId = queryParams["requestId"].ToString();
            var amount = queryParams["amount"].ToString();
            var orderInfo = queryParams["orderInfo"].ToString();
            var orderType = queryParams["orderType"].ToString();
            var transId = queryParams["transId"].ToString();
            var resultCode = queryParams["resultCode"].ToString();
            var message = queryParams["message"].ToString();
            var payType = queryParams["payType"].ToString();
            var responseTime = queryParams["responseTime"].ToString();
            var extraData = queryParams["extraData"].ToString();
            var signature = queryParams["signature"].ToString();

            // Check if payment was successful
            if (resultCode != "0")
                return false;

            // Validate signature
            var secretKey = _configuration["MoMo:SecretKey"];
            var rawSignature = $"accessKey={_configuration["MoMo:AccessKey"]}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            var expectedSignature = HmacSHA256(rawSignature, secretKey ?? "");

            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidateVNPayReturn(IQueryCollection queryParams)
        {
            // VNPay validation logic
            var vnp_ResponseCode = queryParams["vnp_ResponseCode"].ToString();
            var vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();

            // Check if payment was successful
            if (vnp_ResponseCode != "00")
                return false;

            // Create sorted dictionary for signature validation
            var vnpData = new SortedDictionary<string, string>();
            foreach (var param in queryParams)
            {
                if (param.Key.StartsWith("vnp_") && param.Key != "vnp_SecureHash")
                {
                    vnpData.Add(param.Key, param.Value.ToString());
                }
            }

            // Generate signature
            var hashSecret = _configuration["VNPay:HashSecret"];
            var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var expectedHash = HmacSHA512(query, hashSecret ?? "");

            return vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }        private async Task<bool> ValidatePayPalReturn(IQueryCollection queryParams)
        {
            var token = queryParams["token"].ToString(); // PayPal returns 'token' as the order ID
            var payerId = queryParams["PayerID"].ToString();

            _logger.LogInformation("ValidatePayPalReturn - Token: {Token}, PayerID: {PayerID}", token, payerId);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(payerId))
            {
                _logger.LogWarning("ValidatePayPalReturn - Missing token or PayerID");
                return false;
            }

            try
            {
                // Capture the payment using the token (PayPal order ID)
                _logger.LogInformation("Calling PayPal CapturePaymentAsync with token: {Token}", token);
                var success = await _payPalService.CapturePaymentAsync(token, payerId);
                _logger.LogInformation("PayPal capture result: {Success}", success);
                return success;
            }            catch (Exception ex)
            {
                // Log error using token instead of paymentId
                _logger.LogError(ex, "Error capturing PayPal payment: {Token}", token);
                return false;
            }
        }

        private bool ValidateMoMoNotification(HttpRequest request)
        {
            // MoMo IPN validation
            try
            {
                // Read request body
                using var reader = new StreamReader(request.Body);
                var body = reader.ReadToEndAsync().Result;
                
                // Parse JSON and validate signature
                // This is a simplified implementation
                return !string.IsNullOrEmpty(body);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateVNPayNotification(HttpRequest request)
        {
            // VNPay IPN validation
            try
            {
                var vnpData = new SortedDictionary<string, string>();
                foreach (var param in request.Query)
                {
                    if (param.Key.StartsWith("vnp_") && param.Key != "vnp_SecureHash")
                    {
                        vnpData.Add(param.Key, param.Value.ToString());
                    }
                }

                var vnp_SecureHash = request.Query["vnp_SecureHash"].ToString();
                var hashSecret = _configuration["VNPay:HashSecret"];
                var query = string.Join("&", vnpData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var expectedHash = HmacSHA512(query, hashSecret ?? "");

                return vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePayPalNotification(HttpRequest request)
        {
            // PayPal IPN validation
            // In a real implementation, you would verify with PayPal
            return true;
        }

        /// <summary>
        /// Test PayPal payment functionality - For development testing only
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestPayPal()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Create a test booking
            var testBooking = new Booking
            {
                Id = 999999, // Test booking ID
                UserId = user.Id,
                HomestayId = 1, // Assuming homestay ID 1 exists
                CheckInDate = DateTime.Now.AddDays(7),
                CheckOutDate = DateTime.Now.AddDays(10),
                NumberOfGuests = 2,
                TotalAmount = 1500000, // 1.5 million VND
                FinalAmount = 1500000,
                Status = BookingStatus.Paid, // Using Paid as the default status
                CreatedAt = DateTime.Now,
                Homestay = new Homestay 
                { 
                    Id = 1, 
                    Name = "Test Homestay - PayPal Demo",
                    City = "Ho Chi Minh City",
                    PricePerNight = 500000
                }
            };

            // Create a test payment
            var testPayment = new Payment
            {
                Id = 999999, // Test payment ID
                BookingId = testBooking.Id,
                UserId = user.Id,
                Amount = testBooking.FinalAmount,
                PaymentMethod = PaymentMethod.PayPal,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.Now
            };

            try
            {
                var paymentUrl = await _payPalService.CreatePaymentAsync(testPayment, testBooking);
                
                ViewBag.PaymentUrl = paymentUrl;
                ViewBag.VNDAmount = testPayment.Amount.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
                ViewBag.USDAmount = (await _currencyService.ConvertVNDToUSDAsync(testPayment.Amount)).ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                ViewBag.TransactionId = testPayment.TransactionId;
                
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"PayPal payment creation failed: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult PaymentCancel()
        {
            ViewBag.Message = "Thanh toán đã bị hủy bởi người dùng.";
            ViewBag.IsSuccess = false;
            return View("PaymentCancelResult");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCancel(string paymentMethod = "paypal")
        {
            var token = Request.Query["token"].ToString();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Find the payment and mark it as cancelled
                var payment = await _context.Payments
                    .Include(p => p.Booking)
                    .FirstOrDefaultAsync(p => p.TransactionId == token);
                
                if (payment != null && payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            
            TempData["Error"] = "Thanh toán đã bị hủy. Bạn có thể thử lại sau.";
            return RedirectToAction("Index", "Booking");
        }
    }
}

