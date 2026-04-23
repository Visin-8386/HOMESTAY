using WebHS.Data;
using Microsoft.EntityFrameworkCore;

namespace WebHS.Services
{
    public interface IWebsiteInfoService
    {
        Task<string> GetWebsiteStatsAsync();
        Task<string> GetHomestayInfoAsync();
        Task<string> GetPopularHomestaysAsync();
    }

    public class WebsiteInfoService : IWebsiteInfoService
    {
        private readonly ApplicationDbContext _context;

        public WebsiteInfoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetWebsiteStatsAsync()
        {
            try
            {
                var totalHomestays = await _context.Homestays.CountAsync(h => h.IsActive);
                var totalBookings = await _context.Bookings.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var popularCities = await _context.Homestays
                    .Where(h => h.IsActive)
                    .GroupBy(h => h.City)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToListAsync();

                var stats = $@"
THỐNG KÊ ĐOM ĐÓM DREAM HIỆN TẠI:
- Tổng số homestay: {totalHomestays} địa điểm
- Tổng số đặt phòng: {totalBookings} booking
- Tổng số người dùng: {totalUsers} thành viên
- Thành phố phổ biến: {string.Join(", ", popularCities)}
";

                return stats;
            }
            catch (Exception)
            {
                return "Không thể lấy thống kê hiện tại.";
            }
        }

        public async Task<string> GetHomestayInfoAsync()
        {
            try
            {
                var homestays = await _context.Homestays
                    .Where(h => h.IsActive && h.IsApproved)
                    .Include(h => h.Images)
                    .OrderByDescending(h => h.ViewCount)
                    .Take(20)  // Lấy 20 homestay phổ biến nhất
                    .Select(h => new
                    {
                        h.Id,  // Thêm ID để tạo link
                        h.Name,
                        h.City,
                        h.District,
                        h.Address,
                        h.PricePerNight,
                        h.MaxGuests,
                        h.Bedrooms,
                        h.Bathrooms,
                        h.Description,
                        h.ViewCount
                    })
                    .ToListAsync();

                var info = "DANH SÁCH HOMESTAY PHỔ BIẾN:\n\n";
                
                foreach (var homestay in homestays)
                {
                    var priceFormatted = homestay.PricePerNight.ToString("N0");
                    info += $"📍 {homestay.Name}\n";
                    info += $"   📍 Địa chỉ: {homestay.Address}, {homestay.District}, {homestay.City}\n";
                    info += $"   💰 Giá: {priceFormatted} VNĐ/đêm\n";
                    info += $"   👥 Tối đa: {homestay.MaxGuests} khách\n";
                    info += $"   🏠 {homestay.Bedrooms} phòng ngủ, {homestay.Bathrooms} phòng tắm\n";
                    info += $"   � Link: http://localhost:5000/Homestay/Details/{homestay.Id}\n";
                    info += $"   �� Lượt xem: {homestay.ViewCount}\n";
                    if (!string.IsNullOrEmpty(homestay.Description) && homestay.Description.Length > 100)
                    {
                        info += $"   📝 Mô tả: {homestay.Description.Substring(0, 100)}...\n";
                    }
                    info += "\n";
                }

                return info;
            }
            catch (Exception)
            {
                return "Không thể lấy thông tin homestay.";
            }
        }

        public async Task<string> GetPopularHomestaysAsync()
        {
            try
            {
                // Lấy homestay có nhiều booking nhất
                var popularHomestays = await _context.Bookings
                    .Where(b => b.Status == Models.BookingStatus.Completed)
                    .GroupBy(b => b.HomestayId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { HomestayId = g.Key, BookingCount = g.Count() })
                    .ToListAsync();

                var homestayIds = popularHomestays.Select(p => p.HomestayId).ToList();
                var homestayDetails = await _context.Homestays
                    .Where(h => homestayIds.Contains(h.Id) && h.IsActive)
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.City,
                        h.PricePerNight,
                        h.MaxGuests
                    })
                    .ToListAsync();

                var info = "TOP HOMESTAY ĐƯỢC ĐẶT NHIỀU NHẤT:\n\n";
                
                foreach (var popular in popularHomestays)
                {
                    var homestay = homestayDetails.FirstOrDefault(h => h.Id == popular.HomestayId);
                    if (homestay != null)
                    {
                        var priceFormatted = homestay.PricePerNight.ToString("N0");
                        info += $"⭐ {homestay.Name} ({homestay.City})\n";
                        info += $"   💰 {priceFormatted} VNĐ/đêm\n";
                        info += $"   👥 Tối đa {homestay.MaxGuests} khách\n";
                        info += $"   🔗 Link: http://localhost:5000/Homestay/Details/{homestay.Id}\n";
                        info += $"   � {popular.BookingCount} lượt đặt\n\n";
                    }
                }

                return info;
            }
            catch (Exception)
            {
                return "Không thể lấy thông tin homestay phổ biến.";
            }
        }
    }
}
