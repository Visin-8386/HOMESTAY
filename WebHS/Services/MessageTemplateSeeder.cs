using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public class MessageTemplateSeeder
    {
        private readonly ApplicationDbContext _context;

        public MessageTemplateSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Kiểm tra xem đã có templates chưa
            if (await _context.MessageTemplates.AnyAsync())
                return;

            // Lấy một host ID hợp lệ từ AspNetUsers
            var hostId = await _context.Users
                .Where(u => u.UserName == "admin@webhs.com" || u.Email == "admin@webhs.com")
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(hostId))
            {
                // Nếu không có admin, lấy user đầu tiên
                hostId = await _context.Users
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrEmpty(hostId))
            {
                // Không có user nào trong database, thoát
                return;
            }

            var templates = new List<MessageTemplate>
            {
                // Welcome messages
                new MessageTemplate
                {
                    Name = "Chào mừng khách mới",
                    Type = MessageTemplateType.WelcomeMessage,
                    Subject = "Chào mừng bạn đến với {homestayName}!",
                    Content = "Xin chào {guestName},\n\nCảm ơn bạn đã đặt phòng tại {homestayName}! Chúng tôi rất vui được đón tiếp bạn.\n\nThông tin check-in:\n- Thời gian: {checkInTime}\n- Địa chỉ: {address}\n- Liên hệ: {hostPhone}\n\nChúc bạn có chuyến đi vui vẻ!\n\nTrân trọng,\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                },
                new MessageTemplate
                {
                    Name = "Xác nhận check-in",
                    Type = MessageTemplateType.CheckInInstructions,
                    Subject = "Hướng dẫn check-in tại {homestayName}",
                    Content = "Xin chào {guestName},\n\nBạn sẽ check-in vào ngày mai! Dưới đây là thông tin quan trọng:\n\n📍 Địa chỉ: {address}\n🕐 Thời gian check-in: {checkInTime}\n📞 Số điện thoại khẩn cấp: {hostPhone}\n\nHướng dẫn:\n1. Gọi cho tôi khi bạn đến gần\n2. Mang theo CMND/CCCD\n3. Wifi: {wifiName} / Pass: {wifiPassword}\n\nHẹn gặp bạn!\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                },
                new MessageTemplate
                {
                    Name = "Cảm ơn sau check-out",
                    Type = MessageTemplateType.CheckOutReminder,
                    Subject = "Cảm ơn bạn đã lưu trú tại {homestayName}",
                    Content = "Xin chào {guestName},\n\nCảm ơn bạn đã lựa chọn {homestayName} cho chuyến đi của mình! Hy vọng bạn đã có những trải nghiệm tuyệt vời.\n\nNếu bạn hài lòng với dịch vụ, rất mong bạn dành chút thời gian để đánh giá và chia sẻ trải nghiệm.\n\nChúng tôi luôn chào đón bạn quay lại!\n\nTrân trọng,\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                },
                new MessageTemplate
                {
                    Name = "Nhắc nhở thanh toán",
                    Type = MessageTemplateType.BookingConfirmation,
                    Subject = "Nhắc nhở thanh toán cho đặt phòng #{bookingId}",
                    Content = "Xin chào {guestName},\n\nChúng tôi nhận thấy booking #{bookingId} của bạn chưa được thanh toán.\n\nThông tin booking:\n- Homestay: {homestayName}\n- Check-in: {checkInDate}\n- Check-out: {checkOutDate}\n- Tổng tiền: {totalAmount}\n\nVui lòng hoàn tất thanh toán để đảm bảo đặt phòng của bạn.\n\nCảm ơn bạn!\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                },
                new MessageTemplate
                {
                    Name = "Yêu cầu đánh giá",
                    Type = MessageTemplateType.ThankYouMessage,
                    Subject = "Chia sẻ trải nghiệm tại {homestayName}",
                    Content = "Xin chào {guestName},\n\nHy vọng bạn đã có kỳ nghỉ tuyệt vời tại {homestayName}!\n\nTrải nghiệm của bạn rất quan trọng với chúng tôi. Bạn có thể dành 2 phút để đánh giá và chia sẻ cảm nhận không?\n\nĐánh giá của bạn sẽ giúp:\n✨ Cải thiện chất lượng dịch vụ\n✨ Hỗ trợ khách hàng tương lai\n✨ Động viên team chúng tôi\n\nCảm ơn bạn rất nhiều!\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                },
                new MessageTemplate
                {
                    Name = "Thông báo hủy booking",
                    Type = MessageTemplateType.Custom,
                    Subject = "Thông báo hủy booking #{bookingId}",
                    Content = "Xin chào {guestName},\n\nChúng tôi rất tiếc phải thông báo rằng booking #{bookingId} đã bị hủy.\n\nLý do: {cancellationReason}\n\nNếu đây là lỗi của chúng tôi, chúng tôi sẽ hoàn tiền 100% trong vòng 3-5 ngày làm việc.\n\nRất xin lỗi vì sự bất tiện này!\n\nTrân trọng,\n{hostName}",
                    IsActive = true,
                    HostId = hostId,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.MessageTemplates.AddRange(templates);
            await _context.SaveChangesAsync();
        }
    }
}
