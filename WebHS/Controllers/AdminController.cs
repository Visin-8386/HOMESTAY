using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Services; // ADDED: Import services
using WebHS.Attributes; // ADDED: Import custom attributes
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [CustomAuthorize(UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly IHomestayService _homestayService; // ADDED
        private readonly IExcelExportService _excelExportService;
        private readonly IEmailService _emailService; // ADDED

        public AdminController(ApplicationDbContext context, UserManager<WebHSUser> userManager, IHomestayService homestayService, IExcelExportService excelExportService, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _homestayService = homestayService; // ADDED
            _excelExportService = excelExportService;
            _emailService = emailService; // ADDED
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Đếm tất cả user có email hợp lệ và đang hoạt động
                var totalUsers = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && u.IsActive)
                    .CountAsync();
                
                // Đếm tất cả homestay (không phân biệt IsActive)
                var totalHomestays = await _context.Homestays.CountAsync();
                
                // Đếm tất cả booking có trạng thái hợp lệ
                var totalBookings = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .CountAsync();
                
                // Tính tổng doanh thu từ các booking đã thanh toán hoặc hoàn thành
                var totalRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) 
                           && b.FinalAmount > 0)
                    .SumAsync(b => b.FinalAmount);

                // Thống kê khuyến mãi
                var totalPromotions = await _context.Promotions
                    .Where(p => p.IsActive)
                    .CountAsync();
                    
                var activePromotions = await _context.Promotions
                    .CountAsync(p => p.IsActive 
                               && p.StartDate <= DateTime.UtcNow 
                               && p.EndDate >= DateTime.UtcNow);

                // Đếm homestay chờ duyệt
                var pendingHomestays = await _context.Homestays
                    .Where(h => !h.IsApproved && h.IsActive)
                    .CountAsync();

                // Lấy danh sách người dùng mới nhất
                var recentUsers = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Lấy danh sách đặt phòng gần đây
                var recentBookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Dữ liệu biểu đồ doanh thu (6 tháng gần nhất)
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var monthlyRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                           && b.CreatedAt >= sixMonthsAgo
                           && b.FinalAmount > 0)
                    .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                    .Select(g => new
                    {
                        Month = g.Key.Month,
                        Year = g.Key.Year,
                        Revenue = g.Sum(b => b.FinalAmount)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                // Tạo labels và data cho chart (đảm bảo có đủ 6 tháng)
                var chartData = new List<decimal>();
                var chartLabels = new List<string>();
                
                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = DateTime.UtcNow.AddMonths(-i);
                    var monthData = monthlyRevenue.FirstOrDefault(x => x.Year == targetDate.Year && x.Month == targetDate.Month);
                    
                    chartLabels.Add($"{targetDate.Month:D2}/{targetDate.Year}");
                    chartData.Add(monthData?.Revenue ?? 0);
                }

                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalHomestays = totalHomestays,
                    TotalBookings = totalBookings,
                    TotalRevenue = totalRevenue,
                    TotalPromotions = totalPromotions,
                    ActivePromotions = activePromotions,
                    PendingHomestays = pendingHomestays,
                    RecentUsers = recentUsers,
                    RecentBookings = recentBookings,
                    RevenueChartLabels = chartLabels,
                    RevenueChartData = chartData
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                // Log lỗi nếu có logger
                // _logger?.LogError(ex, "Error loading admin dashboard");
                
                // Tạo viewmodel rỗng để tránh crash
                var emptyViewModel = new AdminDashboardViewModel
                {
                    TotalUsers = 0,
                    TotalHomestays = 0,
                    TotalBookings = 0,
                    TotalRevenue = 0,
                    TotalPromotions = 0,
                    ActivePromotions = 0,
                    PendingHomestays = 0,
                    RecentUsers = new List<User>(),
                    RecentBookings = new List<Booking>(),
                    RevenueChartLabels = new List<string>(),
                    RevenueChartData = new List<decimal>()
                };
                
                TempData["Error"] = "Có lỗi xảy ra khi tải dữ liệu dashboard. Vui lòng thử lại sau.";
                return View(emptyViewModel);
            }
        }

        public async Task<IActionResult> Users(string search = "", int page = 1, string role = "")
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => (u.Email != null && u.Email.Contains(search)) || 
                                       u.FirstName.Contains(search) || 
                                       u.LastName.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create user view models with roles
            // Create dictionary of user roles
            var userRolesDictionary = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                userRolesDictionary[user.Id] = userRoles.ToList();
            }

            var viewModel = new AdminUserListViewModel
            {
                Users = users,
                UserRoles = userRolesDictionary,
                SearchTerm = search,
                SelectedRole = role,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
            
            // Also keep the ViewBag for backward compatibility
            ViewBag.UserRoles = viewModel.UserRoles;

            return View(viewModel);
        }

        public async Task<IActionResult> Homestays(string search = "", int page = 1, string status = "")
        {
            var query = _context.Homestays
                .Include(h => h.Host)
                .Include(h => h.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.Name.Contains(search) || 
                                       h.City.Contains(search) || 
                                       h.State.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(h => !h.IsApproved);
                        break;
                    case "approved":
                        query = query.Where(h => h.IsApproved && h.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(h => !h.IsActive);
                        break;
                }
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var homestays = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminHomestayListViewModel
            {
                Homestays = homestays,
                SearchTerm = search,
                SelectedStatus = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            ViewBag.Status = status; // Add ViewBag for status filter display
            return View(viewModel);
        }

        public async Task<IActionResult> HomestayDetails(int id)
        {
            // Sử dụng service để lấy đầy đủ thông tin chi tiết homestay (không filter IsApproved/IsActive cho admin)
            var homestay = await _context.Homestays
                .Include(h => h.Host)
                .Include(h => h.Images)
                .Include(h => h.HomestayAmenities).ThenInclude(ha => ha.Amenity)
                .Include(h => h.Bookings.Where(b => b.ReviewRating.HasValue)).ThenInclude(b => b.User)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (homestay == null)
                return NotFound();

            // Không tăng view count cho admin
            var viewModel = new WebHS.ViewModels.HomestayDetailViewModel
            {
                Homestay = homestay,
                Images = homestay.Images.OrderBy(i => i.Order).ToList(),
                Amenities = homestay.HomestayAmenities.Select(ha => ha.Amenity),
                ReviewBookings = homestay.Bookings.Where(b => b.ReviewRating.HasValue).OrderByDescending(b => b.ReviewCreatedAt),
                AverageRating = homestay.AverageRating,
                ReviewCount = homestay.ReviewCount,
                HostName = (homestay.Host != null) ? $"{homestay.Host.FirstName} {homestay.Host.LastName}" : "N/A", 
                HostEmail = homestay.Host?.Email ?? "N/A",
                HostAvatar = homestay.Host?.ProfilePicture ?? string.Empty,
                CanReview = false // Admin không review
            };

            return View(viewModel);
        }
        
        [HttpPost]
        public async Task<IActionResult> ApproveHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .FirstOrDefaultAsync(h => h.Id == id);
                
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                // Update homestay status
                homestay.IsApproved = true;
                await _context.SaveChangesAsync();
                
                // Send approval notification email to host (non-blocking)
                if (!string.IsNullOrEmpty(homestay.Host?.Email))
                {
                    try
                    {
                        await _emailService.SendHomestayApprovalNotificationAsync(homestay.Host.Email, homestay);
                        return Json(new { success = true, message = "Homestay đã được phê duyệt thành công và email thông báo đã được gửi đến host." });
                    }
                    catch (Exception)
                    {
                        // Ghi log, không ảnh hưởng kết quả
                        return Json(new { success = true, message = "Homestay đã được phê duyệt thành công (email thông báo gặp lỗi)." });
                    }
                }
                else
                {
                    return Json(new { success = true, message = "Homestay đã được phê duyệt thành công." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error approving homestay {id}: {ex.Message}");
                return Json(new { success = false, message = $"Có lỗi xảy ra khi phê duyệt homestay: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RejectHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .FirstOrDefaultAsync(h => h.Id == id);
                
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                // Update homestay status
                homestay.IsActive = false;
                homestay.IsApproved = false;
                await _context.SaveChangesAsync();
                
                // Send rejection notification email to host (non-blocking)
                if (!string.IsNullOrEmpty(homestay.Host?.Email))
                {
                    try
                    {
                        await _emailService.SendHomestayRejectionNotificationAsync(homestay.Host.Email, homestay);
                        return Json(new { success = true, message = "Homestay đã bị từ chối và email thông báo đã được gửi đến host." });
                    }
                    catch (Exception)
                    {
                        // Ghi log, không ảnh hưởng kết quả
                        return Json(new { success = true, message = "Homestay đã bị từ chối (email thông báo gặp lỗi)." });
                    }
                }
                else
                {
                    return Json(new { success = true, message = "Homestay đã bị từ chối." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting homestay {id}: {ex.Message}");
                return Json(new { success = false, message = $"Có lỗi xảy ra khi từ chối homestay: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var wasActive = user.IsActive;
                    user.IsActive = !user.IsActive;
                    await _userManager.UpdateAsync(user);
                    
                    // Send notification email to user
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        try
                        {
                            if (user.IsActive && !wasActive)
                            {
                                // Account was reactivated
                                await _emailService.SendAccountReactivationNotificationAsync(user.Email, user.FullName);
                            }
                            else if (!user.IsActive && wasActive)
                            {
                                // Account was suspended
                                await _emailService.SendAccountSuspensionNotificationAsync(user.Email, user.FullName, "Tài khoản bị tạm khóa bởi quản trị viên");
                            }
                        }
                        catch (Exception)
                        {
                            // Ghi log, không ảnh hưởng kết quả
                        }
                    }
                    
                    TempData["Success"] = $"Trạng thái tài khoản đã được {(user.IsActive ? "kích hoạt" : "khóa")} và email thông báo đã được gửi.";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling user status {id}: {ex.Message}");
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Bookings(string search = "", int page = 1, string status = "")
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Host)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => (b.User.Email != null && b.User.Email.Contains(search)) || 
                                       b.Homestay.Name.Contains(search) ||
                                       b.User.FirstName.Contains(search) || 
                                       b.User.LastName.Contains(search)); 
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminBookingListViewModel
            {
                Bookings = bookings,
                SearchTerm = search,
                SelectedStatus = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            ViewBag.Status = status; // Add ViewBag for status filter display
            return View(viewModel);
        }

        public async Task<IActionResult> Reports()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var monthlyStats = new
            {
                ThisMonthBookings = await _context.Bookings.CountAsync(b => b.CreatedAt >= thisMonth),
                LastMonthBookings = await _context.Bookings.CountAsync(b => b.CreatedAt >= lastMonth && b.CreatedAt < thisMonth),
                ThisMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) && b.CreatedAt >= thisMonth)
                    .SumAsync(b => b.FinalAmount),
                LastMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) && b.CreatedAt >= lastMonth && b.CreatedAt < thisMonth)
                    .SumAsync(b => b.FinalAmount)
            };

            // Top homestays by bookings
            var topHomestays = await _context.Homestays
                .Include(h => h.Bookings)
                .OrderByDescending(h => h.Bookings.Count)
                .Take(10)
                .Select(h => new { h.Name, BookingCount = h.Bookings.Count })
                .ToListAsync();

            // Top hosts by revenue
            var topHosts = await _context.Users
                .Include(u => u.Homestays) 
                    .ThenInclude(h => h.Bookings)
                .Where(u => u.Homestays.Any()) 
                .Select(u => new { 
                    Name = $"{u.FirstName} {u.LastName}", 
                    Revenue = u.Homestays.SelectMany(h => h.Bookings) 
                        .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                        .Sum(b => b.FinalAmount)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            var viewModel = new AdminReportsViewModel
            {
                ThisMonthBookings = monthlyStats.ThisMonthBookings,
                LastMonthBookings = monthlyStats.LastMonthBookings,
                ThisMonthRevenue = monthlyStats.ThisMonthRevenue,
                LastMonthRevenue = monthlyStats.LastMonthRevenue,
                TopHomestays = topHomestays,
                TopHosts = topHosts
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(string userId, List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                
                // Remove all current roles
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }                // Add new roles
                if (roles != null && roles.Any())
                {
                    await _userManager.AddToRolesAsync(user, roles);
                }

                return Json(new { success = true, message = "Cập nhật vai trò thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "Đã khóa tài khoản thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "Đã mở khóa tài khoản thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActivateHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays.FindAsync(id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                homestay.IsActive = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã kích hoạt homestay thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateHomestay(int id)
        {
            try
            {
                var homestay = await _context.Homestays.FindAsync(id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy homestay." });
                }

                homestay.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã vô hiệu hóa homestay thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> UserDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Users));
            }

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);
            
            // Get user's homestays if they are a host
            var homestays = await _context.Homestays
                .Where(h => h.HostId == id)
                .ToListAsync();
            
            // Get user's bookings
            var bookings = await _context.Bookings
                .Include(b => b.Homestay)
                .Where(b => b.UserId == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.UserRoles = roles.ToList();
            ViewBag.Homestays = homestays;
            ViewBag.Bookings = bookings;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRevenueTestData()
        {
            try
            {
                // Lấy user và homestay đầu tiên để test
                var testUser = await _context.Users.FirstOrDefaultAsync();
                var testHomestay = await _context.Homestays.FirstOrDefaultAsync();

                if (testUser == null || testHomestay == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy user hoặc homestay để tạo test data" });
                }

                // Xóa dữ liệu test cũ
                var oldTestBookings = await _context.Bookings
                    .Where(b => b.Notes != null && b.Notes.Contains("REVENUE_TEST"))
                    .ToListAsync();
                
                if (oldTestBookings.Any())
                {
                    _context.Bookings.RemoveRange(oldTestBookings);
                    await _context.SaveChangesAsync();
                }

                var testBookings = new List<Booking>
                {
                    // Booking CheckedIn #1 (3 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-3),
                        CheckOutDate = DateTime.UtcNow.AddDays(2),
                        NumberOfGuests = 2,
                        TotalAmount = 3000000,
                        DiscountAmount = 0,
                        FinalAmount = 3000000,
                        Status = BookingStatus.Paid,
                        Notes = "REVENUE_TEST - CheckedIn booking 1",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedIn #2 (5 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-5),
                        CheckOutDate = DateTime.UtcNow.AddDays(1),
                        NumberOfGuests = 4,
                        TotalAmount = 5000000,
                        DiscountAmount = 0,
                        FinalAmount = 5000000,
                        Status = BookingStatus.Paid,
                        Notes = "REVENUE_TEST - CheckedIn booking 2",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #1 (7 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-10),
                        CheckOutDate = DateTime.UtcNow.AddDays(-7),
                        NumberOfGuests = 3,
                        TotalAmount = 7000000,
                        DiscountAmount = 0,
                        FinalAmount = 7000000,
                        Status = BookingStatus.Completed,
                        Notes = "REVENUE_TEST - CheckedOut booking 1",
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #2 (12 triệu)
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-20),
                        CheckOutDate = DateTime.UtcNow.AddDays(-15),
                        NumberOfGuests = 6,
                        TotalAmount = 12000000,
                        DiscountAmount = 500000,
                        FinalAmount = 11500000,
                        Status = BookingStatus.Completed,
                        Notes = "REVENUE_TEST - CheckedOut booking 2",
                        CreatedAt = DateTime.UtcNow.AddDays(-20),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    },
                    
                    // Booking CheckedOut #3 (4 triệu) - tháng này
                    new Booking
                    {
                        CheckInDate = DateTime.UtcNow.AddDays(-5),
                        CheckOutDate = DateTime.UtcNow.AddDays(-2),
                        NumberOfGuests = 2,
                        TotalAmount = 4000000,
                        DiscountAmount = 0,
                        FinalAmount = 4000000,
                        Status = BookingStatus.Completed,
                        Notes = "REVENUE_TEST - CheckedOut booking 3 this month",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UserId = testUser.Id,
                        HomestayId = testHomestay.Id
                    }
                };

                _context.Bookings.AddRange(testBookings);
                await _context.SaveChangesAsync();

                // Tính toán total revenue để verify
                var totalRevenue = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                    .SumAsync(b => b.FinalAmount);

                var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var thisMonthRevenue = await _context.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) && b.CreatedAt >= thisMonth)
                    .SumAsync(b => b.FinalAmount);

                return Json(new { 
                    success = true, 
                    message = $"Đã tạo {testBookings.Count} booking test. Total Revenue: {totalRevenue:N0} VND, This Month: {thisMonthRevenue:N0} VND",
                    totalRevenue = totalRevenue,
                    thisMonthRevenue = thisMonthRevenue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Host)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }        [HttpPost]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt phòng" });
                }

                // Logic: Nếu booking đã Paid thì không cần confirm lại
                if (booking.Status != BookingStatus.Paid)
                {
                    return Json(new { success = false, message = "Đặt phòng này không thể xác nhận" });
                }

                // Booking đã Paid rồi thì không cần thay đổi gì
                return Json(new { success = true, message = "Đặt phòng đã được xác nhận" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt phòng" });
                }

                if (booking.Status != BookingStatus.Paid)
                {
                    return Json(new { success = false, message = "Đặt phòng này không thể hủy" });
                }

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteBooking(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt phòng" });
                }

                if (booking.Status != BookingStatus.Paid)
                {
                    return Json(new { success = false, message = "Chỉ có thể hoàn thành đặt phòng đã thanh toán" });
                }

                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hoàn thành đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }            var viewModel = new EditBookingViewModel
            {
                Id = booking.Id,
                UserId = booking.UserId,
                HomestayId = booking.HomestayId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                Notes = booking.Notes,
                HomestayName = booking.Homestay.Name,
                UserName = $"{booking.User.FirstName} {booking.User.LastName}",
                UserEmail = booking.User.Email ?? ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooking(EditBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload related data
                var bookingData = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .FirstOrDefaultAsync(b => b.Id == model.Id);
                
                if (bookingData != null)
                {
                    model.HomestayName = bookingData.Homestay.Name;
                    model.UserName = $"{bookingData.User.FirstName} {bookingData.User.LastName}";
                    model.UserEmail = bookingData.User.Email ?? "";
                }
                return View(model);
            }

            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == model.Id);

                if (booking == null)
                {
                    return NotFound();
                }                // Update booking properties
                booking.CheckInDate = model.CheckInDate;
                booking.CheckOutDate = model.CheckOutDate;
                booking.NumberOfGuests = model.NumberOfGuests;
                booking.TotalAmount = model.TotalAmount;
                booking.DiscountAmount = model.DiscountAmount;
                booking.FinalAmount = model.FinalAmount;
                booking.Status = model.Status;
                booking.Notes = model.Notes;
                booking.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã cập nhật đặt phòng thành công!";
                return RedirectToAction("Bookings");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View(model);
            }
        }

        // EXCEL EXPORT ACTIONS        [HttpGet]
        public async Task<IActionResult> ExportUsersToExcel()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                
                // Convert User to UserViewModel with roles
                var userViewModels = new List<UserViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userViewModels.Add(new UserViewModel
                    {
                        Id = user.Id ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim(),
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        EmailConfirmed = user.EmailConfirmed,
                        CreatedAt = user.CreatedAt,
                        IsActive = user.IsActive,
                        Roles = roles.ToList()
                    });
                }
                
                var excelData = _excelExportService.ExportUsersToExcel(userViewModels);
                
                var fileName = $"QuanLyNguoiDung_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Users");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportHomestaysToExcel()
        {
            try
            {
                var homestays = await _context.Homestays
                    .Include(h => h.Host)
                    .ToListAsync();
                var excelData = _excelExportService.ExportHomestaysToExcel(homestays);
                
                var fileName = $"QuanLyHomestay_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Homestays");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportBookingsToExcel()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .ToListAsync();
                var excelData = _excelExportService.ExportBookingsToExcel(bookings);
                
                var fileName = $"QuanLyDatPhong_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Bookings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportConversationsToExcel()
        {
            try
            {
                var conversations = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.LastMessageSender)
                    .Include(c => c.Messages)
                    .ToListAsync();
                var excelData = _excelExportService.ExportConversationsToExcel(conversations);
                
                var fileName = $"QuanLyHoiThoai_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Index", "Messaging");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportMessagesToExcel()
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Conversation)
                    .ToListAsync();
                var excelData = _excelExportService.ExportMessagesToExcel(messages);
                
                var fileName = $"QuanLyTinNhan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Index", "Messaging");
            }
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Users));
            }

            // Không cho phép xóa chính admin đang đăng nhập
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                TempData["Error"] = "Bạn không thể xóa chính tài khoản của mình.";
                return RedirectToAction(nameof(Users));
            }

            // Kiểm tra xem user có phải là admin cuối cùng không
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (allAdmins.Count <= 1)
                {
                    TempData["Error"] = "Không thể xóa admin cuối cùng trong hệ thống.";
                    return RedirectToAction(nameof(Users));
                }
            }            try
            {
                // Xóa các dữ liệu liên quan trước khi xóa user
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. Xóa các tin nhắn
                var messages = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .ToListAsync();
                _context.Messages.RemoveRange(messages);

                // 2. Xóa conversations liên quan
                var conversations = await _context.Conversations
                    .Where(c => c.Messages.Any(m => m.SenderId == userId || m.ReceiverId == userId))
                    .ToListAsync();
                _context.Conversations.RemoveRange(conversations);

                // 3. Xóa các notifications
                var notifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();
                _context.UserNotifications.RemoveRange(notifications);                // 4. Xử lý bookings - chuyển về hệ thống hoặc hủy
                var bookings = await _context.Bookings
                    .Where(b => b.UserId == userId)
                    .ToListAsync();
                
                // Tìm hoặc tạo tài khoản hệ thống để chuyển bookings
                var systemUser = await _userManager.FindByEmailAsync("system@webhs.com");
                if (systemUser == null)
                {
                    // Tạo tài khoản hệ thống nếu chưa có
                    systemUser = new WebHSUser
                    {
                        UserName = "system@webhs.com",
                        Email = "system@webhs.com",
                        FirstName = "Hệ thống",
                        LastName = "WebHS",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        LockoutEnabled = false,
                        IsActive = true
                    };
                    
                    var createResult = await _userManager.CreateAsync(systemUser, "SystemUser123!");
                    if (!createResult.Succeeded)
                    {
                        throw new Exception($"Không thể tạo tài khoản hệ thống: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    }
                }
                
                foreach (var booking in bookings)
                {
                    // Chuyển booking về tài khoản hệ thống và đánh dấu là đã bị xóa
                    booking.UserId = systemUser.Id;
                    booking.Status = BookingStatus.Cancelled;
                    booking.Notes = (booking.Notes ?? "") + $" [Tài khoản khách hàng {user.Email} đã bị xóa bởi quản trị viên - {DateTime.UtcNow:dd/MM/yyyy HH:mm}]";
                }                // 5. Xử lý payments - chuyển về tài khoản hệ thống thay vì xóa
                var payments = await _context.Payments
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
                    
                foreach (var payment in payments)
                {
                    payment.UserId = systemUser.Id;
                    payment.Notes = (payment.Notes ?? "") + $" [Tài khoản gốc {user.Email} đã bị xóa - {DateTime.UtcNow:dd/MM/yyyy HH:mm}]";
                }                // 6. Xử lý homestays nếu user là host - chuyển về hệ thống và deactivate
                var hostHomestays = await _context.Homestays
                    .Where(h => h.HostId == userId)
                    .ToListAsync();

                foreach (var homestay in hostHomestays)
                {
                    homestay.HostId = systemUser.Id; // Chuyển về tài khoản hệ thống
                    homestay.IsActive = false;
                    homestay.IsApproved = false;
                    homestay.Description = (homestay.Description ?? "") + $"\n[Chú ý: Homestay này thuộc về tài khoản {user.Email} đã bị xóa - {DateTime.UtcNow:dd/MM/yyyy HH:mm}]";
                }

                // 7. Xóa user claims và roles trước (thông qua UserManager)
                await _userManager.RemoveFromRolesAsync(user, userRoles);
                var userClaims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in userClaims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }                // Lưu thay đổi database trước khi xóa user
                await _context.SaveChangesAsync();

                // 8. Gửi email thông báo trước khi xóa user
                string userEmail = user.Email ?? "";
                string userName = user.FullName;
                
                // 9. Xóa user khỏi Identity
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    await transaction.CommitAsync();
                    
                    // Gửi email thông báo sau khi xóa thành công
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        try
                        {
                            await _emailService.SendAccountDeletionNotificationAsync(userEmail, userName, "Tài khoản bị xóa bởi quản trị viên");
                        }
                        catch (Exception)
                        {
                            // Ghi log, không ảnh hưởng kết quả
                        }
                    }
                    
                    TempData["SuccessMessage"] = $"Đã xóa tài khoản {userEmail} thành công và gửi email thông báo.";
                }
                else
                {
                    await transaction.RollbackAsync();
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Lỗi khi xóa tài khoản: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra khi xóa tài khoản: {ex.Message}";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "ID người dùng không hợp lệ." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
                return Json(new { success = false, message = "Bạn không thể khóa chính tài khoản của mình." });

            try
            {
                var wasActive = user.IsActive;
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                // Gửi email như cũ
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        if (user.IsActive && !wasActive)
                        {
                            await _emailService.SendAccountReactivationNotificationAsync(user.Email, user.FullName);
                        }
                        else if (!user.IsActive && wasActive)
                        {
                            await _emailService.SendAccountSuspensionNotificationAsync(user.Email, user.FullName, "Tài khoản bị tạm khóa bởi quản trị viên");
                        }
                    }
                    catch (Exception)
                    {
                        // Ghi log, không ảnh hưởng kết quả
                    }
                }
                var status = user.IsActive ? "kích hoạt" : "khóa";
                return Json(new { success = true, message = $"Đã {status} tài khoản {user.Email} thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
        
        // ADMIN HOMESTAY MANAGEMENT
        
        public async Task<IActionResult> CreateHomestay()
        {
            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
                
            ViewBag.Hosts = await _userManager.GetUsersInRoleAsync("Host");
            
            return View();
        }
          [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHomestay(AdminCreateHomestayViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Convert to regular CreateHomestayViewModel for service
                var createModel = new CreateHomestayViewModel
                {
                    Name = model.Name,
                    Description = model.Description,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    District = model.District,
                    Ward = model.Ward,
                    Country = model.Country,
                    ZipCode = model.ZipCode,
                    YouTubeVideoId = model.YouTubeVideoId,
                    PricePerNight = model.PricePerNight,
                    MaxGuests = model.MaxGuests,
                    Bedrooms = model.Bedrooms,
                    Bathrooms = model.Bathrooms,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Images = model.Images,
                    AmenityIds = model.AmenityIds
                };
                
                var result = await _homestayService.CreateHomestayAsync(createModel, model.HostId);
                
                if (result > 0)
                {
                    // Update admin-specific properties
                    var homestay = await _context.Homestays.FindAsync(result);
                    if (homestay != null)
                    {
                        homestay.IsActive = model.IsActive;
                        homestay.IsApproved = model.IsApproved;
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["Success"] = "Homestay đã được tạo thành công!";
                    return RedirectToAction("HomestayDetails", new { id = result });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không thể tạo homestay.");
                }
            }

            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
                
            ViewBag.Hosts = await _userManager.GetUsersInRoleAsync("Host");
            
            return View(model);
        }
        
        public async Task<IActionResult> EditHomestay(int id)
        {
            var homestay = await _homestayService.GetHomestayByIdAsync(id);
            if (homestay == null)
                return NotFound();

            var model = new AdminEditHomestayViewModel
            {
                Id = homestay.Id,
                Name = homestay.Name,
                Description = homestay.Description,
                Address = homestay.Address,
                City = homestay.City ?? string.Empty,
                State = homestay.State ?? string.Empty,
                District = homestay.District ?? string.Empty,
                Ward = homestay.Ward ?? string.Empty,
                Country = homestay.Country ?? string.Empty,
                ZipCode = homestay.ZipCode,
                YouTubeVideoId = homestay.YouTubeVideoId,
                PricePerNight = homestay.PricePerNight,
                MaxGuests = homestay.MaxGuests,
                Bedrooms = homestay.Bedrooms,
                Bathrooms = homestay.Bathrooms,
                Latitude = homestay.Latitude,
                Longitude = homestay.Longitude,
                Rules = homestay.Rules,
                IsActive = homestay.IsActive,
                IsApproved = homestay.IsApproved,
                HostId = homestay.HostId,
                ExistingImages = homestay.Images,
                AmenityIds = homestay.HomestayAmenities.Select(ha => ha.AmenityId).ToArray()
            };

            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
                
            ViewBag.Hosts = await _userManager.GetUsersInRoleAsync("Host");

            return View(model);
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHomestay(AdminEditHomestayViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Convert to regular EditHomestayViewModel for service
                var editModel = new EditHomestayViewModel
                {
                    Id = model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    District = model.District,
                    Ward = model.Ward,
                    Country = model.Country,
                    ZipCode = model.ZipCode,
                    YouTubeVideoId = model.YouTubeVideoId,
                    PricePerNight = model.PricePerNight,
                    MaxGuests = model.MaxGuests,
                    Bedrooms = model.Bedrooms,
                    Bathrooms = model.Bathrooms,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Images = model.Images,
                    ImagesToDelete = model.ImagesToDelete,
                    AmenityIds = model.AmenityIds
                };
                
                var result = await _homestayService.UpdateHomestayAsync(editModel, model.HostId);

                if (result)
                {
                    // Update admin-specific properties
                    var homestay = await _context.Homestays.FindAsync(model.Id);
                    if (homestay != null)
                    {
                        homestay.IsActive = model.IsActive;
                        homestay.IsApproved = model.IsApproved;
                        homestay.Rules = model.Rules;
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["Success"] = "Homestay đã được cập nhật thành công!";
                    return RedirectToAction("HomestayDetails", new { id = model.Id });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không thể cập nhật homestay.");
                }
            }

            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
                
            ViewBag.Hosts = await _userManager.GetUsersInRoleAsync("Host");

            return View(model);
        }
    }
}


