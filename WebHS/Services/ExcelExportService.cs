using OfficeOpenXml;
using OfficeOpenXml.Style;
using WebHS.Models;
using WebHS.ViewModels;
using System.Drawing;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public ExcelExportService()
        {
            // Set EPPlus license context for version 7.x
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public byte[] ExportUsersToExcel(IEnumerable<UserViewModel> users)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Người dùng");

            var headers = new[]
            {
                "ID", "Tên đăng nhập", "Email", "Họ tên", "Điện thoại", "Loại tài khoản",
                "Trạng thái", "Ngày tạo", "Lần đăng nhập cuối", "Số homestay", "Số đặt phòng"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);            int row = 2;
            foreach (var user in users)
            {
                worksheet.Cells[row, 1].Value = user.Id;
                worksheet.Cells[row, 2].Value = user.UserName;
                worksheet.Cells[row, 3].Value = user.Email;
                worksheet.Cells[row, 4].Value = user.FullName;
                worksheet.Cells[row, 5].Value = user.PhoneNumber ?? "N/A";
                worksheet.Cells[row, 6].Value = user.IsHost ? "Có" : "Không"; // Dùng helper property từ roles
                worksheet.Cells[row, 7].Value = user.IsActive ? "Hoạt động" : "Bị khóa";
                worksheet.Cells[row, 8].Value = user.CreatedAt;
                worksheet.Cells[row, 9].Value = "N/A"; // LastLoginAt không có trong model
                worksheet.Cells[row, 10].Value = "N/A"; // Homestays count cần query riêng
                worksheet.Cells[row, 11].Value = "N/A"; // Bookings count cần query riêng
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportHomestaysToExcel(IEnumerable<Homestay> homestays)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Homestay");

            var headers = new[]
            {
                "ID", "Tên", "Địa chỉ", "Thành phố", "Giá/đêm", "Số phòng", "Số khách tối đa",
                "Trạng thái", "Chủ nhà", "Ngày tạo", "Đánh giá", "Số đánh giá"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var homestay in homestays)
            {
                worksheet.Cells[row, 1].Value = homestay.Id;
                worksheet.Cells[row, 2].Value = homestay.Name;
                worksheet.Cells[row, 3].Value = homestay.Address;
                worksheet.Cells[row, 4].Value = homestay.City;
                worksheet.Cells[row, 5].Value = homestay.PricePerNight;
                worksheet.Cells[row, 6].Value = 1; // NumberOfRooms không có trong model  
                worksheet.Cells[row, 7].Value = homestay.MaxGuests;
                worksheet.Cells[row, 8].Value = homestay.IsActive ? "Hoạt động" : "Không hoạt động";
                worksheet.Cells[row, 9].Value = homestay.Host?.UserName ?? "N/A";
                worksheet.Cells[row, 10].Value = homestay.CreatedAt;
                worksheet.Cells[row, 11].Value = homestay.AverageRating;
                worksheet.Cells[row, 12].Value = homestay.ReviewCount; // Sửa từ TotalReviews
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportBookingsToExcel(IEnumerable<Booking> bookings)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Đặt phòng");

            var headers = new[]
            {
                "ID", "Mã đặt phòng", "Homestay", "Khách hàng", "Email khách", "Điện thoại",
                "Ngày nhận phòng", "Ngày trả phòng", "Số đêm", "Số khách", "Tổng tiền",
                "Trạng thái", "Phương thức TT", "Trạng thái TT", "Ngày đặt"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var booking in bookings)
            {
                worksheet.Cells[row, 1].Value = booking.Id;
                worksheet.Cells[row, 2].Value = $"BK{booking.Id:D6}"; // Tạo BookingCode từ ID
                worksheet.Cells[row, 3].Value = booking.Homestay?.Name ?? "N/A";
                worksheet.Cells[row, 4].Value = $"{booking.User?.FirstName} {booking.User?.LastName}";
                worksheet.Cells[row, 5].Value = booking.User?.Email ?? "N/A";
                worksheet.Cells[row, 6].Value = booking.User?.PhoneNumber ?? "N/A";
                worksheet.Cells[row, 7].Value = booking.CheckInDate;
                worksheet.Cells[row, 8].Value = booking.CheckOutDate;
                worksheet.Cells[row, 9].Value = (booking.CheckOutDate - booking.CheckInDate).Days;
                worksheet.Cells[row, 10].Value = booking.NumberOfGuests;
                worksheet.Cells[row, 11].Value = booking.TotalAmount;
                worksheet.Cells[row, 12].Value = GetBookingStatusText(booking.Status);
                worksheet.Cells[row, 13].Value = "N/A"; // PaymentMethod không có
                worksheet.Cells[row, 14].Value = "N/A"; // PaymentStatus không có
                worksheet.Cells[row, 15].Value = booking.CreatedAt;
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportPromotionsToExcel(IEnumerable<WebHSPromotion> promotions)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Khuyến mãi");

            var headers = new[]
            {
                "ID", "Mã", "Tên", "Mô tả", "Loại", "Giá trị", "Giá trị tối đa",
                "Đơn hàng tối thiểu", "Ngày bắt đầu", "Ngày kết thúc", "Số lần sử dụng",
                "Giới hạn sử dụng", "Trạng thái", "Ngày tạo"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var promotion in promotions)
            {
                worksheet.Cells[row, 1].Value = promotion.Id;
                worksheet.Cells[row, 2].Value = promotion.Code;
                worksheet.Cells[row, 3].Value = promotion.Name;
                worksheet.Cells[row, 4].Value = promotion.Description ?? "N/A";
                worksheet.Cells[row, 5].Value = GetPromotionTypeText(promotion.Type);
                worksheet.Cells[row, 6].Value = promotion.Value;
                worksheet.Cells[row, 7].Value = promotion.MaxDiscountAmount?.ToString() ?? "N/A";
                worksheet.Cells[row, 8].Value = promotion.MinOrderAmount?.ToString() ?? "N/A";
                worksheet.Cells[row, 9].Value = promotion.StartDate;
                worksheet.Cells[row, 10].Value = promotion.EndDate;
                worksheet.Cells[row, 11].Value = promotion.UsedCount; // Sửa từ UsageCount
                worksheet.Cells[row, 12].Value = promotion.UsageLimit?.ToString() ?? "Không giới hạn";
                worksheet.Cells[row, 13].Value = promotion.IsActive ? "Hoạt động" : "Không hoạt động";
                worksheet.Cells[row, 14].Value = promotion.CreatedAt;
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportMonthlyRevenueToExcel(IEnumerable<MonthlyRevenueData> revenueData)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Báo cáo Doanh thu theo tháng");

            var headers = new[]
            {
                "Tháng", "Năm", "Doanh thu"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.Gold);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var data in revenueData)
            {
                worksheet.Cells[row, 1].Value = data.Month;
                worksheet.Cells[row, 2].Value = data.Year;
                worksheet.Cells[row, 3].Value = data.Revenue;
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportConversationsToExcel(IEnumerable<Conversation> conversations)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Hội thoại");

            var headers = new[]
            {
                "ID", "Người 1", "Người 2", "Số tin nhắn",
                "Tin nhắn cuối", "Ngày tạo", "Cập nhật cuối", "Trạng thái"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var conversation in conversations)
            {
                worksheet.Cells[row, 1].Value = conversation.Id;
                worksheet.Cells[row, 2].Value = conversation.User1?.UserName ?? "N/A";
                worksheet.Cells[row, 3].Value = conversation.User2?.UserName ?? "N/A";
                worksheet.Cells[row, 4].Value = conversation.Messages?.Count ?? 0;
                worksheet.Cells[row, 5].Value = conversation.LastMessage?.Substring(0, Math.Min(50, conversation.LastMessage.Length)) ?? "N/A";
                worksheet.Cells[row, 6].Value = conversation.CreatedAt;
                worksheet.Cells[row, 7].Value = conversation.LastMessageAt;
                worksheet.Cells[row, 8].Value = conversation.IsArchived ? "Đã lưu trữ" : "Hoạt động";
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportMessagesToExcel(IEnumerable<Message> messages)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Quản lý Tin nhắn");

            var headers = new[]
            {
                "ID", "Hội thoại ID", "Người gửi", "Nội dung", "Loại tin nhắn",
                "Đã đọc", "Ngày gửi", "Cập nhật cuối"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightSalmon);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var message in messages)
            {
                worksheet.Cells[row, 1].Value = message.Id;
                worksheet.Cells[row, 2].Value = message.ConversationId?.ToString() ?? "N/A";
                worksheet.Cells[row, 3].Value = message.Sender?.UserName ?? "N/A";
                worksheet.Cells[row, 4].Value = message.Content?.Substring(0, Math.Min(100, message.Content.Length)) ?? "N/A";
                worksheet.Cells[row, 5].Value = GetMessageTypeText(message.Type);
                worksheet.Cells[row, 6].Value = message.IsRead ? "Đã đọc" : "Chưa đọc";
                worksheet.Cells[row, 7].Value = message.SentAt;
                worksheet.Cells[row, 8].Value = message.ReadAt?.ToString() ?? "N/A";
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportHostBookingsToExcel(IEnumerable<BookingDetailViewModel> bookings)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Đặt phòng Host");

            var headers = new[]
            {
                "ID", "Khách hàng", "Email", "Homestay", "Ngày nhận phòng", "Ngày trả phòng",
                "Số khách", "Tổng tiền", "Giảm giá", "Thành tiền", "Trạng thái"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using var headerRange = worksheet.Cells[1, 1, 1, headers.Length];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightCyan);
            headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            int row = 2;
            foreach (var booking in bookings)
            {
                worksheet.Cells[row, 1].Value = booking.Id;
                worksheet.Cells[row, 2].Value = booking.UserName;
                worksheet.Cells[row, 3].Value = booking.UserEmail;
                worksheet.Cells[row, 4].Value = booking.HomestayName;
                worksheet.Cells[row, 5].Value = booking.CheckInDate;
                worksheet.Cells[row, 6].Value = booking.CheckOutDate;
                worksheet.Cells[row, 7].Value = booking.NumberOfGuests;
                worksheet.Cells[row, 8].Value = booking.TotalAmount;
                worksheet.Cells[row, 9].Value = booking.DiscountAmount;
                worksheet.Cells[row, 10].Value = booking.FinalAmount;
                worksheet.Cells[row, 11].Value = GetBookingStatusText(booking.Status);
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportHostRevenueToExcel(HostRevenueViewModel revenueData)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Doanh thu Host");

            worksheet.Cells[1, 1].Value = "BÁO CÁO DOANH THU HOST";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            
            worksheet.Cells[3, 1].Value = "Tổng doanh thu:";
            worksheet.Cells[3, 2].Value = revenueData.TotalRevenue;
            worksheet.Cells[4, 1].Value = "Doanh thu tháng này:";
            worksheet.Cells[4, 2].Value = revenueData.ThisMonthRevenue;
            worksheet.Cells[5, 1].Value = "Doanh thu tháng trước:";
            worksheet.Cells[5, 2].Value = revenueData.LastMonthRevenue;

            // Add monthly revenue data
            if (revenueData.MonthlyRevenue.Any())
            {
                worksheet.Cells[7, 1].Value = "DOANH THU THEO THÁNG";
                worksheet.Cells[7, 1].Style.Font.Bold = true;
                
                worksheet.Cells[8, 1].Value = "Tháng";
                worksheet.Cells[8, 2].Value = "Năm";
                worksheet.Cells[8, 3].Value = "Doanh thu";
                
                using var headerRange = worksheet.Cells[8, 1, 8, 3];
                headerRange.Style.Font.Bold = true;
                
                int row = 9;
                foreach (var monthData in revenueData.MonthlyRevenue)
                {
                    worksheet.Cells[row, 1].Value = monthData.Month;
                    worksheet.Cells[row, 2].Value = monthData.Year;
                    worksheet.Cells[row, 3].Value = monthData.Revenue;
                    row++;
                }
            }

            worksheet.Cells[3, 1, 6, 1].Style.Font.Bold = true;
            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        private string GetBookingStatusText(BookingStatus status)
        {
            return status switch            {
                BookingStatus.Paid => "Đã thanh toán",
                BookingStatus.Completed => "Hoàn thành", 
                BookingStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private string GetPaymentStatusText(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Chờ thanh toán",
                PaymentStatus.Processing => "Đang xử lý",
                PaymentStatus.Completed => "Đã thanh toán",
                PaymentStatus.Failed => "Thanh toán thất bại",
                PaymentStatus.Cancelled => "Đã hủy",
                PaymentStatus.Refunded => "Đã hoàn tiền",
                _ => "Không xác định"
            };
        }

        private string GetPromotionTypeText(PromotionType type)
        {
            return type switch
            {
                PromotionType.Percentage => "Phần trăm",
                PromotionType.FixedAmount => "Số tiền cố định",
                _ => "Không xác định"
            };
        }

        private string GetMessageTypeText(MessageType type)
        {
            return type switch
            {
                MessageType.Text => "Văn bản",
                MessageType.Image => "Hình ảnh",
                MessageType.File => "Tập tin",
                MessageType.System => "Hệ thống",
                _ => "Không xác định"
            };
        }
    }
}
