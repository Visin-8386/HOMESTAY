using MailKit.Net.Smtp;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSUser = WebHS.Models.User;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace WebHS.Services
{    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;
        public bool EnableEmailSending { get; set; } = true;
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendConfirmationEmailAsync(string to, string confirmationLink);
        Task SendResetPasswordEmailAsync(string to, string resetLink);
        Task SendBookingConfirmationAsync(string to, string guestName, string homestayName, DateTime checkIn, DateTime checkOut);
        Task SendDetailedBookingConfirmationAsync(string to, WebHS.Models.Booking booking);
        Task SendBookingNotificationToHostAsync(string hostEmail, WebHS.Models.Booking booking);
        Task SendHomestayApprovalNotificationAsync(string hostEmail, WebHS.Models.Homestay homestay);
        Task SendHomestayRejectionNotificationAsync(string hostEmail, WebHS.Models.Homestay homestay);
        Task SendAccountSuspensionNotificationAsync(string userEmail, string userName, string reason = "");
        Task SendAccountReactivationNotificationAsync(string userEmail, string userName);
        Task SendAccountDeletionNotificationAsync(string userEmail, string userName, string reason = "");
        Task SendCustomNotificationToUserAsync(string userEmail, string userName, string subject, string message);
    }    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment hostEnvironment, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _hostEnvironment = hostEnvironment;
            _logger = logger;        }        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            // Check if email sending is disabled
            if (!_emailSettings.EnableEmailSending)
            {
                _logger.LogInformation("Email sending is disabled. Skipping email to {Email} with subject: {Subject}", to, subject);
                return;
            }

            await SendEmailWithRetryAsync(to, subject, body, isHtml, _emailSettings.MaxRetryAttempts);
        }private async Task SendEmailWithRetryAsync(string to, string subject, string body, bool isHtml = true, int maxRetries = 3)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                try
                {
                    attempt++;
                    _logger.LogInformation("Attempting to send email to {Email} (Attempt {Attempt}/{MaxAttempts})", to, attempt, maxRetries + 1);
                    
                    // In development, log email details
                    if (_hostEnvironment.IsDevelopment())
                    {
                        _logger.LogInformation("[DEV MODE] Attempting to send email to: {Email}", to);
                        _logger.LogInformation("[DEV MODE] Subject: {Subject}", subject);
                        _logger.LogInformation("[DEV MODE] SMTP Config: {Server}:{Port}", _emailSettings.SmtpServer, _emailSettings.SmtpPort);
                        _logger.LogInformation("[DEV MODE] Username: {Username}", _emailSettings.SmtpUsername);
                        _logger.LogInformation("[DEV MODE] Password length: {PasswordLength}", _emailSettings.SmtpPassword?.Length ?? 0);
                    }

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                    message.To.Add(new MailboxAddress("", to));
                    message.Subject = subject;

                    var bodyBuilder = new BodyBuilder();
                    if (isHtml)
                        bodyBuilder.HtmlBody = body;
                    else
                        bodyBuilder.TextBody = body;

                    message.Body = bodyBuilder.ToMessageBody();

                    using var client = new SmtpClient();
                    
                    // Fix SMTP connection for port 587 (use STARTTLS) vs 465 (use SSL)
                    SecureSocketOptions secureOptions;
                    if (_emailSettings.SmtpPort == 465)
                    {
                        secureOptions = SecureSocketOptions.SslOnConnect;
                    }
                    else if (_emailSettings.SmtpPort == 587)
                    {
                        secureOptions = SecureSocketOptions.StartTls;
                    }
                    else
                    {
                        secureOptions = _emailSettings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
                    }
                    
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, secureOptions);
                    
                    // Only authenticate if credentials are provided
                    if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) && !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
                    {
                        await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                    }
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    
                    _logger.LogInformation("✅ Email sent successfully to {Email} on attempt {Attempt}", to, attempt);
                    return; // Success, exit the retry loop
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("❌ Email sending failed on attempt {Attempt}/{MaxAttempts} to {Email}: {Error}", 
                        attempt, maxRetries + 1, to, ex.Message);
                    
                    if (_hostEnvironment.IsDevelopment())
                    {
                        _logger.LogError("❌ Inner exception: {InnerException}", ex.InnerException?.Message);
                    }                    // If this was the last attempt, don't wait
                    if (attempt <= maxRetries)
                    {
                        // Wait before retry using configured delay
                        var delayMs = _emailSettings.RetryDelaySeconds * 1000 * attempt; // 2s, 4s, 6s
                        _logger.LogInformation("⏳ Waiting {DelayMs}ms before retry...", delayMs);
                        await Task.Delay(delayMs);
                    }
                }
            }

            // All retries failed
            _logger.LogError("❌ Failed to send email to {Email} after {MaxAttempts} attempts. Last error: {Error}", 
                to, maxRetries + 1, lastException?.Message);
            
            // Only throw in development environment
            if (_hostEnvironment.IsDevelopment() && lastException != null)
            {
                throw new InvalidOperationException($"Failed to send email to {to} after {maxRetries + 1} attempts", lastException);
            }
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationLink)
        {
            var subject = "🏠 Xác nhận tài khoản - HomestayBooking";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Xác nhận tài khoản</title>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }}
                        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.1); }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; text-align: center; }}
                        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
                        .header p {{ margin: 10px 0 0 0; opacity: 0.9; font-size: 16px; }}
                        .content {{ padding: 40px 30px; }}
                        .welcome {{ font-size: 18px; color: #333; margin-bottom: 25px; line-height: 1.6; }}
                        .btn-container {{ text-align: center; margin: 35px 0; }}
                        .btn {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 16px 35px; text-decoration: none; border-radius: 50px; font-weight: bold; font-size: 16px; box-shadow: 0 8px 25px rgba(102,126,234,0.3); transition: all 0.3s ease; }}
                        .btn:hover {{ transform: translateY(-2px); box-shadow: 0 12px 30px rgba(102,126,234,0.4); }}
                        .backup-link {{ background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 25px 0; border-left: 4px solid #667eea; }}
                        .backup-link p {{ margin: 0; color: #666; font-size: 14px; }}
                        .backup-link a {{ color: #667eea; word-break: break-all; }}
                        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; font-size: 14px; }}
                        .features {{ display: flex; justify-content: space-around; margin: 30px 0; }}
                        .feature {{ text-align: center; flex: 1; padding: 0 15px; }}
                        .feature-icon {{ font-size: 24px; margin-bottom: 10px; }}
                        .feature h3 {{ font-size: 16px; margin: 10px 0 5px 0; color: #333; }}
                        .feature p {{ font-size: 14px; color: #666; margin: 0; }}
                    </style>
                </head>
                <body>
                    <div style='padding: 40px 20px;'>
                        <div class='container'>
                            <div class='header'>
                                <h1>🏠 Chào mừng đến với HomestayBooking!</h1>
                                <p>Cảm ơn bạn đã tham gia cộng đồng của chúng tôi</p>
                            </div>
                            
                            <div class='content'>
                                <div class='welcome'>
                                    <p>Xin chào!</p>
                                    <p>Chúng tôi rất vui mừng chào đón bạn gia nhập HomestayBooking - nền tảng đặt phòng homestay hàng đầu tại Việt Nam!</p>
                                    <p>Để hoàn tất việc đăng ký và bắt đầu trải nghiệm những dịch vụ tuyệt vời của chúng tôi, vui lòng xác nhận tài khoản bằng cách nhấp vào nút bên dưới:</p>
                                </div>
                                
                                <div class='btn-container'>
                                    <a href='{confirmationLink}' class='btn'>✅ Xác nhận tài khoản ngay</a>
                                </div>
                                
                                <div class='features'>
                                    <div class='feature'>
                                        <div class='feature-icon'>🏡</div>
                                        <h3>Hàng nghìn homestay</h3>
                                        <p>Khám phá đa dạng lựa chọn</p>
                                    </div>
                                    <div class='feature'>
                                        <div class='feature-icon'>💳</div>
                                        <h3>Thanh toán an toàn</h3>
                                        <p>Đa dạng phương thức</p>
                                    </div>
                                    <div class='feature'>
                                        <div class='feature-icon'>⭐</div>
                                        <h3>Đánh giá minh bạch</h3>
                                        <p>Từ khách hàng thực tế</p>
                                    </div>
                                </div>
                                
                                <div class='backup-link'>
                                    <p><strong>Không thể nhấp vào nút trên?</strong></p>
                                    <p>Sao chép và dán link sau vào trình duyệt của bạn:</p>
                                    <a href='{confirmationLink}'>{confirmationLink}</a>
                                </div>
                                
                                <p style='color: #666; font-size: 14px; margin-top: 30px;'>
                                    <strong>Lưu ý:</strong> Link xác nhận này sẽ hết hạn sau 24 giờ. Nếu bạn không thực hiện xác nhận trong thời gian này, vui lòng đăng ký lại hoặc liên hệ với chúng tôi để được hỗ trợ.
                                </p>
                            </div>
                            
                            <div class='footer'>
                                <p><strong>HomestayBooking</strong> - Nền tảng đặt phòng homestay hàng đầu</p>
                                <p>📧 Email: support@homestaybooking.vn | 📞 Hotline: 1900-xxxx</p>
                                <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                                    Email này được gửi tự động, vui lòng không reply.
                                </p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendResetPasswordEmailAsync(string to, string resetLink)
        {
            var subject = "🔐 Đặt lại mật khẩu - HomestayBooking";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Đặt lại mật khẩu</title>
                    <style>
                        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); }}
                        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.1); }}
                        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 40px 30px; text-align: center; }}
                        .header h1 {{ margin: 0; font-size: 28px; font-weight: bold; }}
                        .header p {{ margin: 10px 0 0 0; opacity: 0.9; font-size: 16px; }}
                        .content {{ padding: 40px 30px; }}
                        .message {{ font-size: 16px; color: #333; margin-bottom: 25px; line-height: 1.6; }}
                        .btn-container {{ text-align: center; margin: 35px 0; }}
                        .btn {{ display: inline-block; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 16px 35px; text-decoration: none; border-radius: 50px; font-weight: bold; font-size: 16px; box-shadow: 0 8px 25px rgba(240,147,251,0.3); transition: all 0.3s ease; }}
                        .btn:hover {{ transform: translateY(-2px); box-shadow: 0 12px 30px rgba(240,147,251,0.4); }}
                        .warning-box {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 20px; border-radius: 10px; margin: 25px 0; }}
                        .backup-link {{ background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 25px 0; border-left: 4px solid #f093fb; }}
                        .backup-link p {{ margin: 0; color: #666; font-size: 14px; }}
                        .backup-link a {{ color: #f093fb; word-break: break-all; }}
                        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #666; font-size: 14px; }}
                        .security-tips {{ background: #e3f2fd; border-left: 4px solid #2196f3; padding: 20px; margin: 25px 0; }}
                        .security-tips h3 {{ margin: 0 0 10px 0; color: #1976d2; font-size: 16px; }}
                        .security-tips ul {{ margin: 0; padding-left: 20px; }}
                        .security-tips li {{ margin: 5px 0; color: #424242; }}
                    </style>
                </head>
                <body>
                    <div style='padding: 40px 20px;'>
                        <div class='container'>
                            <div class='header'>
                                <h1>🔐 Đặt lại mật khẩu</h1>
                                <p>Yêu cầu đặt lại mật khẩu cho tài khoản của bạn</p>
                            </div>
                            
                            <div class='content'>
                                <div class='message'>
                                    <p>Xin chào!</p>
                                    <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản HomestayBooking của bạn.</p>
                                    <p>Để đặt lại mật khẩu, vui lòng nhấp vào nút bên dưới:</p>
                                </div>
                                
                                <div class='btn-container'>
                                    <a href='{resetLink}' class='btn'>🔒 Đặt lại mật khẩu ngay</a>
                                </div>
                                
                                <div class='warning-box'>
                                    <p><strong>⚠️ Lưu ý bảo mật:</strong></p>
                                    <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                                        <li>Link này chỉ có hiệu lực trong <strong>24 giờ</strong></li>
                                        <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                                        <li>Không chia sẻ link này với bất kỳ ai</li>
                                    </ul>
                                </div>
                                
                                <div class='backup-link'>
                                    <p><strong>Không thể nhấp vào nút trên?</strong></p>
                                    <p>Sao chép và dán link sau vào trình duyệt của bạn:</p>
                                    <a href='{resetLink}'>{resetLink}</a>
                                </div>
                                
                                <div class='security-tips'>
                                    <h3>💡 Mẹo bảo mật mật khẩu:</h3>
                                    <ul>
                                        <li>Sử dụng mật khẩu dài ít nhất 8 ký tự</li>
                                        <li>Kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt</li>
                                        <li>Không sử dụng thông tin cá nhân dễ đoán</li>
                                        <li>Không sử dụng lại mật khẩu cũ</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class='footer'>
                                <p><strong>HomestayBooking</strong> - Nền tảng đặt phòng homestay hàng đầu</p>
                                <p>📧 Email: support@homestaybooking.vn | 📞 Hotline: 1900-xxxx</p>
                                <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                                    Email này được gửi tự động, vui lòng không reply.
                                </p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendBookingConfirmationAsync(string to, string guestName, string homestayName, DateTime checkIn, DateTime checkOut)
        {
            var subject = "Xác nhận đặt phòng thành công - HomestayBooking";
            var body = $@"
                <h2>Xác nhận đặt phòng thành công!</h2>
                <p>Chào {guestName},</p>
                <p>Đặt phòng của bạn đã được xác nhận thành công!</p>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                    <h3>Chi tiết đặt phòng:</h3>
                    <p><strong>Homestay:</strong> {homestayName}</p>
                    <p><strong>Ngày nhận phòng:</strong> {checkIn:dd/MM/yyyy}</p>
                    <p><strong>Ngày trả phòng:</strong> {checkOut:dd/MM/yyyy}</p>
                </div>
                <p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của chúng tôi!</p>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendDetailedBookingConfirmationAsync(string to, WebHS.Models.Booking booking)
        {
            var subject = $"Xác nhận đặt phòng #{booking.Id} - WebHS Homestay";
            var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .booking-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .price-breakdown {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .total {{ font-size: 18px; font-weight: bold; color: #667eea; border-top: 2px solid #eee; padding-top: 15px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .status {{ display: inline-block; padding: 8px 16px; border-radius: 20px; color: white; font-weight: bold; }}
        .status.confirmed {{ background-color: #28a745; }}
        .status.pending {{ background-color: #ffc107; color: #333; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏠 Xác nhận đặt phòng thành công!</h1>
            <p>Cảm ơn bạn đã đặt phòng tại WebHS Homestay</p>
        </div>
        
        <div class='content'>
            <p>Chào <strong>{booking.User?.FullName ?? "Quý khách"}</strong>,</p>
            <p>Đặt phòng của bạn đã được xác nhận thành công. Dưới đây là thông tin chi tiết:</p>
            
            <div class='booking-details'>
                <h3>🏡 Thông tin Homestay</h3>
                <p><strong>Tên Homestay:</strong> {booking.Homestay?.Name}</p>
                <p><strong>Địa chỉ:</strong> {booking.Homestay?.Address}</p>
                <p><strong>Địa điểm:</strong> {booking.Homestay?.District}, {booking.Homestay?.City}</p>
                
                <h3>📅 Thời gian lưu trú</h3>
                <p><strong>Ngày nhận phòng:</strong> {booking.CheckInDate:dddd, dd/MM/yyyy} (14:00)</p>
                <p><strong>Ngày trả phòng:</strong> {booking.CheckOutDate:dddd, dd/MM/yyyy} (12:00)</p>
                <p><strong>Số đêm:</strong> {numberOfNights} đêm</p>
                <p><strong>Số khách:</strong> {booking.NumberOfGuests} người</p>
                
                <h3>👤 Thông tin khách hàng</h3>
                <p><strong>Họ tên:</strong> {booking.User?.FullName}</p>
                <p><strong>Email:</strong> {booking.User?.Email}</p>
                <p><strong>Số điện thoại:</strong> {booking.User?.PhoneNumber}</p>
                
                <h3>📋 Mã đặt phòng</h3>
                <p style='font-size: 24px; font-weight: bold; color: #667eea; text-align: center; background: #f0f8ff; padding: 15px; border-radius: 8px; letter-spacing: 2px;'>#{booking.Id:D6}</p>
                
                <h3>🏷️ Trạng thái</h3>
                <p><span class='status {(booking.Status == WebHS.Models.BookingStatus.Paid ? "confirmed" : "pending")}'>{GetBookingStatusText(booking.Status)}</span></p>
            </div>
            
            <div class='price-breakdown'>
                <h3>💰 Chi phí</h3>
                <p><strong>Giá phòng/đêm:</strong> {booking.Homestay?.PricePerNight:N0} VNĐ</p>
                <p><strong>Số đêm:</strong> {numberOfNights}</p>
                <p><strong>Tạm tính:</strong> {(booking.Homestay?.PricePerNight * numberOfNights):N0} VNĐ</p>
                {(booking.DiscountAmount > 0 ? $"<p><strong>Giảm giá:</strong> -{booking.DiscountAmount:N0} VNĐ</p>" : "")}
                <div class='total'>
                    <p>Tổng cộng: {booking.TotalAmount:N0} VNĐ</p>
                </div>
            </div>
            
            <div style='background: #e3f2fd; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <h3>ℹ️ Lưu ý quan trọng</h3>
                <ul>
                    <li>Vui lòng mang theo giấy tờ tuy thân khi nhận phòng</li>
                    <li>Thời gian nhận phòng: 14:00 - 22:00</li>
                    <li>Thời gian trả phòng: 06:00 - 12:00</li>
                    <li>Nếu cần hỗ trợ, vui lòng liên hệ host qua hệ thống tin nhắn</li>
                    <li>Mã đặt phòng cần thiết để checkin</li>
                </ul>
            </div>
            
            <div class='footer'>
                <p>🙏 Cảm ơn bạn đã chọn WebHS Homestay!</p>
                <p>Chúc bạn có một kỳ nghỉ tuyệt vời!</p>
                <p><small>Email này được gửi tự động. Vui lòng không reply.</small></p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(to, subject, body);
        }        private string GetBookingStatusText(WebHS.Models.BookingStatus status)
        {
            return status switch
            {
                WebHS.Models.BookingStatus.Paid => "Đã thanh toán",
                WebHS.Models.BookingStatus.Cancelled => "Đã hủy",
                WebHS.Models.BookingStatus.Completed => "Hoàn thành",
                _ => "Không xác định"
            };
        }

        public async Task SendBookingNotificationToHostAsync(string hostEmail, WebHS.Models.Booking booking)
        {
            var subject = $"🎉 Bạn có đặt phòng mới #{booking.Id} - WebHS Homestay";
            var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .booking-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745; }}
        .guest-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff; }}
        .revenue {{ background: #e8f5e8; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .highlight {{ color: #28a745; font-weight: bold; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #28a745; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Congratulations! Bạn có đặt phòng mới!</h1>
            <p>Homestay {booking.Homestay?.Name} vừa được đặt phòng</p>
        </div>
        
        <div class='content'>
            <p>Chào <strong>Host</strong>,</p>
            <p>🎊 Chúc mừng! Homestay <strong class='highlight'>{booking.Homestay?.Name}</strong> của bạn vừa nhận được một đặt phòng mới.</p>
            
            <div class='booking-details'>
                <h3>📋 Chi tiết đặt phòng</h3>
                <p><strong>Mã đặt phòng:</strong> #{booking.Id}</p>
                <p><strong>Homestay:</strong> {booking.Homestay?.Name}</p>
                <p><strong>📅 Check-in:</strong> {booking.CheckInDate:dddd, dd/MM/yyyy} (14:00)</p>
                <p><strong>📅 Check-out:</strong> {booking.CheckOutDate:dddd, dd/MM/yyyy} (12:00)</p>
                <p><strong>🌙 Số đêm:</strong> {numberOfNights} đêm</p>
                <p><strong>👥 Số khách:</strong> {booking.NumberOfGuests} người</p>
                <p><strong>💰 Trạng thái:</strong> <span class='highlight'>Đã thanh toán</span></p>
            </div>
            
            <div class='guest-info'>
                <h3>👤 Thông tin khách hàng</h3>
                <p><strong>Họ tên:</strong> {booking.User?.FullName}</p>                <p><strong>Email:</strong> {booking.User?.Email}</p>
                <p><strong>Số điện thoại:</strong> {booking.User?.PhoneNumber ?? "Chưa cung cấp"}</p>
                {(string.IsNullOrEmpty(booking.Notes) ? "" : $"<p><strong>Ghi chú:</strong> {booking.Notes}</p>")}
            </div>
            
            <div class='revenue'>
                <h3>💰 Doanh thu từ đặt phòng này</h3>
                <div class='amount'>{booking.TotalAmount:N0} VNĐ</div>
                <p>Đã được thanh toán và xác nhận</p>
            </div>
            
            <div style='background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                <h4>📞 Liên hệ với khách hàng</h4>
                <p>Bạn có thể liên hệ trực tiếp với khách hàng qua:</p>
                <ul>
                    <li>Email: {booking.User?.Email}</li>
                    {(string.IsNullOrEmpty(booking.User?.PhoneNumber) ? "" : $"<li>SĐT: {booking.User?.PhoneNumber}</li>")}
                    <li>Hệ thống tin nhắn trên WebHS</li>
                </ul>
            </div>
            
            <div style='background: #d1ecf1; padding: 15px; border-radius: 8px; border-left: 4px solid #bee5eb; margin: 20px 0;'>
                <h4>📝 Chuẩn bị đón khách</h4>
                <p>Để đảm bảo trải nghiệm tốt nhất cho khách hàng:</p>
                <ul>
                    <li>✅ Chuẩn bị phòng sạch sẽ, đầy đủ tiện nghi</li>
                    <li>✅ Kiểm tra và cập nhật thông tin liên lạc</li>
                    <li>✅ Chuẩn bị hướng dẫn check-in/check-out</li>
                    <li>✅ Liên hệ khách trước ngày đến nếu cần</li>
                </ul>
            </div>
            
            <div class='footer'>
                <p>Cảm ơn bạn đã là đối tác của WebHS Homestay! 🙏</p>
                <p>Chúc bạn có những trải nghiệm hosting tuyệt vời!</p>
                <p>---</p>
                <p>Đội ngũ WebHS Homestay</p>
                <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(hostEmail, subject, body, true);
        }

        public async Task SendHomestayApprovalNotificationAsync(string hostEmail, WebHS.Models.Homestay homestay)
        {
            var subject = $"✅ Homestay của bạn đã được phê duyệt - {homestay.Name}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Chúc mừng! Homestay của bạn đã được phê duyệt</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>Host</strong>,</p>
            <p>🎉 Chúc mừng! Homestay của bạn <strong>{homestay.Name}</strong> đã được phê duyệt và sẵn sàng để đón tiếp khách.</p>
              <h3>📋 Thông tin homestay</h3>
            <p><strong>Tên homestay:</strong> {homestay.Name}</p>
            <p><strong>Địa chỉ:</strong> {homestay.Address}</p>
            
            <h3>🔗 Liên kết quản lý homestay</h3>
            <p>Bạn có thể quản lý thông tin homestay, xem đặt phòng và liên hệ với khách hàng qua trang quản lý của chúng tôi:</p>
            <p><a href='https://admin.webhshomestay.com' style='color: #28a745; font-weight: bold;'>Đến trang quản lý homestay</a></p>
            
            <p>Cảm ơn bạn đã tin tưởng và hợp tác với WebHS Homestay. Chúng tôi mong chờ được đồng hành cùng bạn!</p>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(hostEmail, subject, body, true);
        }

        public async Task SendHomestayRejectionNotificationAsync(string hostEmail, WebHS.Models.Homestay homestay)
        {
            var subject = $"❌ Homestay của bạn đã bị từ chối - {homestay.Name}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Rất tiếc! Homestay của bạn đã bị từ chối</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>Host</strong>,</p>
            <p>Chúng tôi rất tiếc phải thông báo rằng homestay của bạn <strong>{homestay.Name}</strong> đã bị từ chối do không đáp ứng được một số tiêu chí cần thiết.</p>
            
            <h3>📋 Thông tin homestay</h3>
            <p><strong>Tên homestay:</strong> {homestay.Name}</p>
            <p><strong>Địa chỉ:</strong> {homestay.Address}</p>
            
            <h3>🔍 Lý do từ chối</h3>
            <p>Homestay của bạn không đáp ứng được một số yêu cầu về tiêu chuẩn chất lượng hoặc chính sách của chúng tôi.</p>
            
            <h3>📈 Cải thiện và đăng ký lại</h3>
            <p>Chúng tôi khuyến khích bạn cải thiện chất lượng homestay và đăng ký lại để được xem xét phê duyệt.</p>
            
            <p>Cảm ơn bạn đã quan tâm và hợp tác với WebHS Homestay.</p>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(hostEmail, subject, body, true);
        }

        public async Task SendAccountSuspensionNotificationAsync(string userEmail, string userName, string reason = "")
        {
            var subject = "⚠️ Tài khoản của bạn đã bị tạm khóa - WebHS Homestay";
            var reasonText = string.IsNullOrEmpty(reason) ? "Vi phạm chính sách sử dụng" : reason;
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ff6b35 0%, #f7931e 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .warning {{ background: #fff3cd; padding: 15px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Tài khoản đã bị tạm khóa</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>{userName}</strong>,</p>
            <p>Chúng tôi rất tiếc phải thông báo rằng tài khoản của bạn trên WebHS Homestay đã bị tạm thời khóa.</p>
            
            <div class='warning'>
                <h4>📋 Lý do khóa tài khoản</h4>
                <p>{reasonText}</p>
            </div>
            
            <h3>🔒 Tác động của việc khóa tài khoản</h3>
            <ul>
                <li>❌ Không thể đăng nhập vào hệ thống</li>
                <li>❌ Không thể đặt phòng homestay</li>
                <li>❌ Không thể quản lý homestay (nếu là host)</li>
                <li>❌ Không thể sử dụng các dịch vụ của WebHS</li>
            </ul>
            
            <h3>📞 Liên hệ để mở khóa</h3>
            <p>Nếu bạn cho rằng việc khóa tài khoản là nhầm lẫn hoặc muốn khiếu nại, vui lòng liên hệ với chúng tôi:</p>
            <ul>
                <li>📧 Email: support@webhshomestay.com</li>
                <li>📞 Hotline: 1900-xxxx</li>
                <li>🕒 Thời gian hỗ trợ: 8:00 - 22:00 hàng ngày</li>
            </ul>
            
            <p>Chúng tôi sẽ xem xét và phản hồi trong vòng 24-48 giờ.</p>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(userEmail, subject, body, true);
        }

        public async Task SendAccountReactivationNotificationAsync(string userEmail, string userName)
        {
            var subject = "✅ Tài khoản của bạn đã được kích hoạt lại - WebHS Homestay";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .success {{ background: #d4edda; padding: 15px; border-radius: 8px; border-left: 4px solid #28a745; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng bạn trở lại!</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>{userName}</strong>,</p>
            <p>Chúng tôi vui mừng thông báo rằng tài khoản của bạn trên WebHS Homestay đã được kích hoạt lại!</p>
            
            <div class='success'>
                <h4>✅ Tài khoản đã được mở khóa</h4>
                <p>Bạn có thể sử dụng lại tất cả các dịch vụ của WebHS như bình thường.</p>
            </div>
            
            <h3>🔓 Bạn có thể làm gì bây giờ</h3>
            <ul>
                <li>✅ Đăng nhập vào hệ thống</li>
                <li>✅ Đặt phòng homestay</li>
                <li>✅ Quản lý homestay (nếu là host)</li>
                <li>✅ Sử dụng đầy đủ các dịch vụ của WebHS</li>
            </ul>
            
            <h3>🔗 Đăng nhập ngay</h3>
            <p>Hãy đăng nhập để tiếp tục trải nghiệm dịch vụ của chúng tôi:</p>
            <p><a href='http://localhost:5000/Account/Login' style='display: inline-block; background: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đăng nhập ngay</a></p>
            
            <p>Cảm ơn bạn đã tiếp tục tin tưởng và sử dụng dịch vụ WebHS Homestay!</p>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(userEmail, subject, body, true);
        }

        public async Task SendAccountDeletionNotificationAsync(string userEmail, string userName, string reason = "")
        {
            var subject = "🗑️ Tài khoản của bạn đã bị xóa - WebHS Homestay";
            var reasonText = string.IsNullOrEmpty(reason) ? "Vi phạm nghiêm trọng chính sách sử dụng" : reason;
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .danger {{ background: #f8d7da; padding: 15px; border-radius: 8px; border-left: 4px solid #dc3545; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🗑️ Tài khoản đã bị xóa</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>{userName}</strong>,</p>
            <p>Chúng tôi rất tiếc phải thông báo rằng tài khoản của bạn trên WebHS Homestay đã bị xóa vĩnh viễn.</p>
            
            <div class='danger'>
                <h4>📋 Lý do xóa tài khoản</h4>
                <p>{reasonText}</p>
            </div>
            
            <h3>⚠️ Thông tin quan trọng</h3>
            <ul>
                <li>🗑️ Tài khoản đã bị xóa vĩnh viễn</li>
                <li>📊 Tất cả dữ liệu liên quan đã bị xóa</li>
                <li>🏠 Các homestay đã bị gỡ khỏi hệ thống</li>
                <li>📅 Các đặt phòng hiện tại đã bị hủy</li>
                <li>❌ Không thể khôi phục tài khoản</li>
            </ul>
            
            <h3>📞 Liên hệ hỗ trợ</h3>
            <p>Nếu bạn cho rằng việc xóa tài khoản là nhầm lẫn, vui lòng liên hệ với chúng tôi trong vòng 7 ngày:</p>
            <ul>
                <li>📧 Email: support@webhshomestay.com</li>
                <li>📞 Hotline: 1900-xxxx</li>
                <li>🕒 Thời gian hỗ trợ: 8:00 - 22:00 hàng ngày</li>
            </ul>
            
            <p>Cảm ơn bạn đã từng sử dụng dịch vụ WebHS Homestay.</p>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(userEmail, subject, body, true);
        }

        public async Task SendCustomNotificationToUserAsync(string userEmail, string userName, string subject, string message)
        {
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff 0%, #0056b3 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .message {{ background: #e7f3ff; padding: 20px; border-radius: 8px; border-left: 4px solid #007bff; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📢 Thông báo từ WebHS Homestay</h1>
        </div>
        
        <div class='content'>
            <p>Chào <strong>{userName}</strong>,</p>
            
            <div class='message'>
                <p>{message.Replace("\n", "<br>")}</p>
            </div>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi:</p>
            <ul>
                <li>📧 Email: support@webhshomestay.com</li>
                <li>📞 Hotline: 1900-xxxx</li>
                <li>🕒 Thời gian hỗ trợ: 8:00 - 22:00 hàng ngày</li>
            </ul>
        </div>
        
        <div class='footer'>
            <p>Đội ngũ WebHS Homestay</p>
            <p>📧 Email: support@webhshomestay.com | 📞 Hotline: 1900-xxxx</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(userEmail, subject, body, true);
        }
    }
}

