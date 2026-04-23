using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Attributes; // ADDED: Import custom attributes
using WebHSUserRoles = WebHS.Models.UserRoles;
using System.Text;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [CustomAuthorize(UserRoles.Host)]
    public class HostController : Controller
    {
        private readonly IHomestayService _homestayService;
        private readonly IBookingService _bookingService;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IExcelExportService _excelExportService;

        public HostController(
            IHomestayService homestayService,
            IBookingService bookingService,
            UserManager<WebHSUser> userManager,
            ApplicationDbContext context,
            IExcelExportService excelExportService)
        {
            _homestayService = homestayService;
            _bookingService = bookingService;
            _userManager = userManager;
            _context = context;
            _excelExportService = excelExportService;        }
        
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;
              // Lấy tất cả homestays của host
            var homestays = await _context.Homestays
                .Where(h => h.HostId == userId)
                .Include(h => h.Images)
                .ToListAsync();

            // Lấy tất cả bookings của host
            var allBookings = await _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == userId)
                .ToListAsync();

            // Thống kê tổng quan
            var totalHomestays = homestays.Count;
            var activeHomestays = homestays.Count(h => h.IsActive && h.IsApproved);
            var pendingHomestays = homestays.Count(h => !h.IsApproved);
            
            var totalBookings = allBookings.Count;
            var completedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed);
            var totalRevenue = allBookings
                .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                .Sum(b => b.FinalAmount);

            // Thống kê tháng hiện tại
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = allBookings
                .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) &&
                           b.CreatedAt.Month == currentMonth && b.CreatedAt.Year == currentYear)
                .Sum(b => b.FinalAmount);            // Tính conversion rate với ViewCount thực tế
            var totalViews = homestays.Sum(h => h.ViewCount);
            var monthlyViews = totalViews; // Có thể tính monthly views riêng nếu cần
            var bookingConversionRate = totalViews > 0 ? (double)totalBookings / totalViews * 100 : 0;            // Top homestays theo performance
            var topHomestays = homestays.Select(h => new HomestayPerformanceViewModel
            {
                Id = h.Id,
                Name = h.Name,
                Image = h.Images.FirstOrDefault()?.ImageUrl ?? "/images/no-image.jpg",
                TotalBookings = allBookings.Count(b => b.HomestayId == h.Id),
                TotalRevenue = allBookings
                    .Where(b => b.HomestayId == h.Id && (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed))
                    .Sum(b => b.FinalAmount),
                Rating = h.AverageRating,
                Views = h.ViewCount,
                // Tính tỷ lệ doanh thu của homestay này trên tổng doanh thu của host
                ConversionRate = totalRevenue > 0 ? (double)allBookings
                    .Where(b => b.HomestayId == h.Id && (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed))
                    .Sum(b => b.FinalAmount) / (double)totalRevenue * 100 : 0
            }).OrderByDescending(h => h.TotalRevenue).Take(5).ToList();

            // Doanh thu 12 tháng gần nhất
            var monthlyRevenueData = new List<MonthlyRevenueViewModel>();
            for (int i = 11; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                var monthRevenue = allBookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) &&
                               b.CreatedAt.Month == targetDate.Month && b.CreatedAt.Year == targetDate.Year)
                    .Sum(b => b.FinalAmount);
                
                monthlyRevenueData.Add(new MonthlyRevenueViewModel
                {
                    Year = targetDate.Year,
                    Month = targetDate.Month,
                    MonthName = targetDate.ToString("MM/yyyy"),
                    Revenue = monthRevenue,
                    BookingCount = allBookings.Count(b => 
                        b.CreatedAt.Month == targetDate.Month && b.CreatedAt.Year == targetDate.Year)
                });
            }

            // Booking data 12 tháng gần nhất
            var monthlyBookingData = new List<MonthlyBookingViewModel>();
            for (int i = 11; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                var monthBookings = allBookings
                    .Where(b => b.CreatedAt.Month == targetDate.Month && b.CreatedAt.Year == targetDate.Year)
                    .ToList();
                
                monthlyBookingData.Add(new MonthlyBookingViewModel
                {
                    Year = targetDate.Year,
                    Month = targetDate.Month,
                    MonthName = targetDate.ToString("MM/yyyy"),
                    BookingCount = monthBookings.Count,
                    CompletedCount = monthBookings.Count(b => b.Status == BookingStatus.Completed),
                    CancelledCount = monthBookings.Count(b => b.Status == BookingStatus.Cancelled)
                });
            }

            // Upcoming bookings
            var upcomingBookings = allBookings
                .Where(b => b.CheckInDate >= DateTime.Today && b.Status == BookingStatus.Paid)
                .OrderBy(b => b.CheckInDate)
                .Take(10)
                .ToList();            // Calendar data - các ngày đã được đặt
            var bookedDates = allBookings
                .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                .SelectMany(b => GetDateRange(b.CheckInDate, b.CheckOutDate)
                    .Select(date => new BookedDateViewModel
                    {
                        HomestayId = b.HomestayId,
                        HomestayName = b.Homestay.Name,
                        Date = date,
                        GuestName = b.User?.FullName ?? "Unknown",
                        Status = b.Status
                    }))
                .ToList();            var viewModel = new HostDashboardViewModel
            {
                TotalHomestays = totalHomestays,
                ActiveHomestays = activeHomestays,
                PendingHomestays = pendingHomestays,
                TotalBookings = totalBookings,
                ActiveBookings = allBookings.Count(b => b.Status == BookingStatus.Paid),
                PendingBookings = 0, // Tạm thời set 0 vì không có Pending status
                CompletedBookings = completedBookings,
                TotalRevenue = totalRevenue,
                TotalEarnings = totalRevenue,
                ThisMonthEarnings = monthlyRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalViews = totalViews,
                MonthlyViews = monthlyViews,
                BookingConversionRate = Math.Round(bookingConversionRate, 2),
                TopHomestays = topHomestays,
                MonthlyRevenueData = monthlyRevenueData,
                MonthlyBookingData = monthlyBookingData,
                UpcomingBookings = upcomingBookings,
                RecentBookings = allBookings.Take(5).Select(b => new BookingDetailViewModel 
                { 
                    Booking = b,
                    HomestayName = b.Homestay?.Name ?? "Unknown"
                }),                MyHomestays = homestays.Select(h => new HomestayCardViewModel
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    PrimaryImage = h.Images.FirstOrDefault()?.ImageUrl ?? "/images/no-image.jpg"
                }),
                BookedDates = bookedDates,
                RevenueChartLabels = monthlyRevenueData.Select(x => x.MonthName).ToList(),
                RevenueChartData = monthlyRevenueData.Select(x => x.Revenue).ToList()
            };

            return View(viewModel);
        }

        private IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                yield return date;            }
        }

        [HttpGet]
        public async Task<IActionResult> Homestays()
        {
            var userId = _userManager.GetUserId(User)!;
            var homestays = await _homestayService.GetHostHomestaysAsync(userId);
            return View(homestays);
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Lấy tất cả reviews cho homestays của host
            var reviews = await _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == userId && b.ReviewRating.HasValue)                .OrderByDescending(b => b.ReviewCreatedAt)
                .Select(b => new HostReviewViewModel
                {
                    Id = b.Id,
                    HomestayName = b.Homestay.Name,
                    GuestName = b.User.FullName,
                    Rating = b.ReviewRating ?? 0,
                    Comment = b.ReviewComment ?? "",
                    CreatedAt = b.ReviewCreatedAt ?? DateTime.MinValue,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate
                })
                .ToListAsync();

            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> AllBookings(string status = "all", int page = 1, int? homestayFilter = null)
        {
            var userId = _userManager.GetUserId(User)!;
            const int pageSize = 10;

            // Query bookings cho host
            var query = _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == userId);

            // Filter theo homestay
            if (homestayFilter.HasValue)
            {
                query = query.Where(b => b.HomestayId == homestayFilter.Value);
            }

            // Filter theo status
            if (status != "all")
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            // Phân trang
            var totalBookings = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy danh sách homestays cho filter
            var homestays = await _context.Homestays
                .Where(h => h.HostId == userId)
                .Select(h => new { h.Id, h.Name })
                .ToListAsync();

            var viewModel = new AllBookingsViewModel
            {
                Bookings = bookings,
                CurrentStatus = status,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalBookings / pageSize),
                TotalBookings = totalBookings,
                HomestayFilter = homestayFilter,
                Homestays = homestays.Select(h => new SelectListItem 
                { 
                    Value = h.Id.ToString(), 
                    Text = h.Name 
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateHomestay()
        {
            return RedirectToAction("Create", "Homestay");
        }

        [HttpGet]
        public async Task<IActionResult> ManageBookings(string status = "all", int page = 1, int? homestayFilter = null)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetHostBookingsAsync(userId, status, page);
            
            // If homestayFilter is provided, filter the bookings by specific homestay
            if (homestayFilter.HasValue)
            {
                bookings.Bookings = bookings.Bookings.Where(b => b.Booking.HomestayId == homestayFilter.Value);
                ViewBag.HomestayFilter = homestayFilter.Value;
                
                // Get homestay name for display
                var homestay = await _context.Homestays.FindAsync(homestayFilter.Value);
                ViewBag.HomestayName = homestay?.Name ?? "Homestay không tìm thấy";
            }
            
            ViewBag.Status = status;
            return View("Bookings", bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var result = await _bookingService.ConfirmBookingAsync(id);
            
            if (result)
            {
                TempData["Message"] = "Đã xác nhận đặt phòng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xác nhận đặt phòng.";
            }

            return RedirectToAction(nameof(ManageBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var result = await _bookingService.CancelBookingAsync(id, _userManager.GetUserId(User)!);
            
            if (result)
            {
                TempData["Message"] = "Đã từ chối đặt phòng thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể từ chối đặt phòng.";
            }

            return RedirectToAction(nameof(ManageBookings));
        }

        [HttpGet]
        public IActionResult Bookings(string status = "all", int page = 1, int? homestayId = null)
        {
            // If homestayId is provided, we need to filter by specific homestay
            if (homestayId.HasValue)
            {
                return RedirectToAction(nameof(ManageBookings), new { status, page, homestayFilter = homestayId.Value });
            }
            // Redirect to the main ManageBookings action to maintain URL consistency
            return RedirectToAction(nameof(ManageBookings), new { status, page });
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            var userId = _userManager.GetUserId(User)!;
            var homestays = await _homestayService.GetHostHomestaysAsync(userId);
            var hostBookings = await _bookingService.GetHostBookingsAsync(userId, "all", 1);
            
            var revenueModel = new HostRevenueViewModel
            {
                TotalRevenue = hostBookings.Bookings.Where(b => b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed).Sum(b => b.Booking.FinalAmount),
                ThisMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed) && 
                    b.Booking.CreatedAt.Month == DateTime.UtcNow.Month && 
                    b.Booking.CreatedAt.Year == DateTime.UtcNow.Year).Sum(b => b.Booking.FinalAmount),
                LastMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed) && 
                    b.Booking.CreatedAt.Month == DateTime.UtcNow.AddMonths(-1).Month && 
                    b.Booking.CreatedAt.Year == DateTime.UtcNow.AddMonths(-1).Year).Sum(b => b.Booking.FinalAmount),
                RevenueByHomestay = homestays.ToDictionary(h => h.Name, h => 
                    hostBookings.Bookings.Where(b => b.Booking.HomestayId == h.Id && (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed))
                    .Sum(b => b.Booking.FinalAmount))
            };
            
            return View(revenueModel);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Simple dashboard for Index view
            var homestays = await _context.Homestays
                .Where(h => h.HostId == userId)
                .ToListAsync();

            var bookings = await _context.Bookings
                .Include(b => b.Homestay)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == userId)
                .ToListAsync();

            var activeBookings = bookings.Count(b => b.Status == BookingStatus.Paid);
            var totalEarnings = bookings
                .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                .Sum(b => b.FinalAmount);

            var thisMonth = DateTime.Now;
            var thisMonthEarnings = bookings
                .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed) &&
                           b.CreatedAt.Month == thisMonth.Month && b.CreatedAt.Year == thisMonth.Year)
                .Sum(b => b.FinalAmount);

            // Create simple ViewModel for Index view (using old structure)
            var viewModel = new
            {
                TotalHomestays = homestays.Count,
                ActiveBookings = activeBookings,
                TotalBookings = bookings.Count,
                PendingBookings = bookings.Count(b => b.Status == BookingStatus.Paid),
                TotalEarnings = totalEarnings,
                ThisMonthEarnings = thisMonthEarnings,
                TotalRevenue = totalEarnings,
                RecentBookings = bookings.OrderByDescending(b => b.CreatedAt).Take(5),
                MyHomestays = homestays.Take(5),
                RevenueChartLabels = new List<string>(),
                RevenueChartData = new List<decimal>()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            // Redirect to the main Account Profile page
            return RedirectToAction("Profile", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingDetail(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // FIXED: Query specific booking directly instead of pagination search
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.Homestay.HostId == userId);
            
            if (booking == null)
                return NotFound();

            // Convert to BookingDetailViewModel for the partial view
            var bookingDetail = new BookingDetailViewModel
            {
                Id = booking.Id,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                Booking = booking,
                HomestayName = booking.Homestay.Name,
                HomestayLocation = $"{booking.Homestay.City}, {booking.Homestay.State}",
                PrimaryImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl ?? "/images/placeholder-homestay.svg",
                UserName = $"{booking.User.FirstName} {booking.User.LastName}",
                UserEmail = booking.User.Email ?? "",
                UserPhone = booking.User.PhoneNumber ?? "",
                CanReview = false,
                CanCancel = booking.Status == BookingStatus.Paid,
                HomestayImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl ?? "/images/placeholder-homestay.svg"
            };

            return PartialView("_BookingDetailPartial", bookingDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInBooking(int id)
        {
            // Since IBookingService doesn't have CheckInBookingAsync, we'll update status manually
            var result = await UpdateBookingStatusAsync(id, BookingStatus.Paid);
            
            return Json(new { 
                success = result, 
                message = result ? "Đã check-in thành công!" : "Không thể check-in đặt phòng này." 
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            // Use existing UpdateBookingStatusAsync method
            var result = await UpdateBookingStatusAsync(id, BookingStatus.Completed);
            
            return Json(new { 
                success = result, 
                message = result ? "Đã hoàn thành đặt phòng!" : "Không thể hoàn thành đặt phòng này." 
            });
        }

        // Helper method to update booking status
        private async Task<bool> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                
                // Verify the booking belongs to this host's homestay
                var booking = await _context.Bookings
                    .Include(b => b.Homestay)
                    .FirstOrDefaultAsync(b => b.Id == bookingId && b.Homestay.HostId == userId);

                if (booking == null)
                    return false;

                // Validate status transition
                if (!IsValidStatusTransition(booking.Status, newStatus))
                    return false;

                booking.Status = newStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }        private static bool IsValidStatusTransition(BookingStatus currentStatus, BookingStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (BookingStatus.Paid, BookingStatus.Paid) => true,
                (BookingStatus.Paid, BookingStatus.Completed) => true,
                (BookingStatus.Completed, BookingStatus.Completed) => true,
                _ => false
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, bool isActive)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var homestay = await _context.Homestays
                    .FirstOrDefaultAsync(h => h.Id == id && h.HostId == userId);

                if (homestay == null)
                    return Json(new { success = false, message = "Không tìm thấy homestay." });

                homestay.IsActive = isActive;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = isActive ? "Đã kích hoạt homestay." : "Đã tạm dừng homestay." 
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BookingDetail(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.Homestay.HostId == userId);
            
            if (booking == null)
                return NotFound();

            var bookingDetail = new BookingDetailViewModel
            {
                Id = booking.Id,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                Booking = booking,
                HomestayName = booking.Homestay.Name,
                HomestayLocation = $"{booking.Homestay.City}, {booking.Homestay.State}",
                PrimaryImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl ?? "/images/placeholder-homestay.svg",
                UserName = $"{booking.User.FirstName} {booking.User.LastName}",
                UserEmail = booking.User.Email ?? "",
                UserPhone = booking.User.PhoneNumber ?? "",
                CanReview = false,
                CanCancel = booking.Status == BookingStatus.Paid,
                HomestayImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl ?? "/images/placeholder-homestay.svg"
            };

            return View("BookingDetail", bookingDetail);
        }

        // EXCEL EXPORT ACTIONS
        [HttpGet]
        public async Task<IActionResult> ExportHomestaysToExcel()
        {
            try
            {
                var hostId = _userManager.GetUserId(User);
                var homestays = await _context.Homestays
                    .Include(h => h.Host)
                    .Where(h => h.HostId == hostId)
                    .ToListAsync();
                var excelData = _excelExportService.ExportHomestaysToExcel(homestays);
                
                var fileName = $"Homestay_CuaToi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportBookingsToExcel()
        {
            try
            {
                var hostId = _userManager.GetUserId(User);
                var hostHomestayIds = await _context.Homestays
                    .Where(h => h.HostId == hostId)
                    .Select(h => h.Id)
                    .ToListAsync();

                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Homestay)
                    .Where(b => hostHomestayIds.Contains(b.HomestayId))
                    .ToListAsync();
                
                var excelData = _excelExportService.ExportBookingsToExcel(bookings);
                
                var fileName = $"DatPhong_CuaToi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("ManageBookings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportHostRevenueToExcel()
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var homestays = await _homestayService.GetHostHomestaysAsync(userId);
                var hostBookings = await _bookingService.GetHostBookingsAsync(userId, "all", 1);
                
                var revenueModel = new HostRevenueViewModel
                {
                    TotalRevenue = hostBookings.Bookings.Where(b => b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed).Sum(b => b.Booking.FinalAmount),
                    ThisMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed) && 
                        b.Booking.CreatedAt.Month == DateTime.UtcNow.Month && 
                        b.Booking.CreatedAt.Year == DateTime.UtcNow.Year).Sum(b => b.Booking.FinalAmount),
                    LastMonthRevenue = hostBookings.Bookings.Where(b => (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed) && 
                        b.Booking.CreatedAt.Month == DateTime.UtcNow.AddMonths(-1).Month && 
                        b.Booking.CreatedAt.Year == DateTime.UtcNow.AddMonths(-1).Year).Sum(b => b.Booking.FinalAmount),
                    RevenueByHomestay = homestays.ToDictionary(h => h.Name, h => 
                        hostBookings.Bookings.Where(b => b.Booking.HomestayId == h.Id && (b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed))
                        .Sum(b => b.Booking.FinalAmount)),
                    
                    // Generate monthly revenue data for the last 6 months  
                    MonthlyRevenue = hostBookings.Bookings
                        .Where(b => b.Booking.Status == BookingStatus.Paid || b.Booking.Status == BookingStatus.Completed)
                        .Where(b => b.Booking.CreatedAt >= DateTime.UtcNow.AddMonths(-6))
                        .GroupBy(b => new { b.Booking.CreatedAt.Year, b.Booking.CreatedAt.Month })
                        .Select(g => new MonthlyRevenueData
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Revenue = g.Sum(b => b.Booking.FinalAmount)
                        })
                        .OrderBy(x => x.Year).ThenBy(x => x.Month)
                        .ToList()
                };
                
                var excelData = _excelExportService.ExportHostRevenueToExcel(revenueModel);
                
                var fileName = $"BaoCaoDoanhThu_Host_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xuất Excel: " + ex.Message;
                return RedirectToAction("Revenue");
            }
        }

        // Message Templates Management
        [HttpGet]
        public async Task<IActionResult> MessageTemplates(string searchTerm = "", MessageTemplateType? selectedType = null)
        {
            var userId = _userManager.GetUserId(User)!;
            
            var query = _context.MessageTemplates
                .Where(mt => mt.HostId == userId);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(mt => mt.Name.Contains(searchTerm) || mt.Subject.Contains(searchTerm));
            }

            if (selectedType.HasValue)
            {
                query = query.Where(mt => mt.Type == selectedType.Value);
            }

            var templates = await query.OrderByDescending(mt => mt.CreatedAt).ToListAsync();

            var viewModel = new MessageTemplateListViewModel
            {
                Templates = templates.Select(mt => new MessageTemplateViewModel
                {
                    Id = mt.Id,
                    Name = mt.Name,
                    Subject = mt.Subject,
                    Content = mt.Content,
                    Type = mt.Type,
                    IsActive = mt.IsActive,
                    CreatedAt = mt.CreatedAt
                }).ToList(),
                SearchTerm = searchTerm,
                SelectedType = selectedType
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateMessageTemplate()
        {
            return View(new CreateMessageTemplateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessageTemplate(CreateMessageTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User)!;
                
                var template = new MessageTemplate
                {
                    Name = model.Name,
                    Subject = model.Subject,
                    Content = model.Content,
                    Type = model.Type,
                    IsActive = model.IsActive,
                    HostId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MessageTemplates.Add(template);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tin nhắn mẫu đã được tạo thành công!";
                return RedirectToAction(nameof(MessageTemplates));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditMessageTemplate(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

            if (template == null)
            {
                TempData["Error"] = "Không tìm thấy tin nhắn mẫu.";
                return RedirectToAction(nameof(MessageTemplates));
            }

            var model = new CreateMessageTemplateViewModel
            {
                Name = template.Name,
                Subject = template.Subject,
                Content = template.Content,
                Type = template.Type,
                IsActive = template.IsActive
            };

            ViewBag.TemplateId = id;
            return View("CreateMessageTemplate", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessageTemplate(int id, CreateMessageTemplateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User)!;
                var template = await _context.MessageTemplates
                    .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

                if (template == null)
                {
                    TempData["Error"] = "Không tìm thấy tin nhắn mẫu.";
                    return RedirectToAction(nameof(MessageTemplates));
                }

                template.Name = model.Name;
                template.Subject = model.Subject;
                template.Content = model.Content;
                template.Type = model.Type;
                template.IsActive = model.IsActive;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Tin nhắn mẫu đã được cập nhật thành công!";
                return RedirectToAction(nameof(MessageTemplates));
            }

            ViewBag.TemplateId = id;
            return View("CreateMessageTemplate", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessageTemplate(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

            if (template != null)
            {
                _context.MessageTemplates.Remove(template);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tin nhắn mẫu đã được xóa thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTemplateStatus(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

            if (template != null)
            {
                template.IsActive = !template.IsActive;
                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = template.IsActive ? 
                        "Mẫu tin nhắn đã được kích hoạt!" : 
                        "Mẫu tin nhắn đã được tạm dừng!",
                    isActive = template.IsActive
                });
            }

            return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu." });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessageTemplate(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

            if (template == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu." });
            }

            return Json(new { 
                success = true, 
                template = new {
                    id = template.Id,
                    name = template.Name,
                    subject = template.Subject,
                    content = template.Content,
                    type = template.Type,
                    isActive = template.IsActive
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> DuplicateMessageTemplate(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var originalTemplate = await _context.MessageTemplates
                .FirstOrDefaultAsync(mt => mt.Id == id && mt.HostId == userId);

            if (originalTemplate == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu." });
            }

            var duplicatedTemplate = new MessageTemplate
            {
                Name = $"{originalTemplate.Name} (Bản sao)",
                Subject = originalTemplate.Subject,
                Content = originalTemplate.Content,
                Type = originalTemplate.Type,
                IsActive = false, // Bản sao mặc định tạm dừng
                HostId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MessageTemplates.Add(duplicatedTemplate);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Tin nhắn mẫu đã được sao chép thành công!",
                newId = duplicatedTemplate.Id
            });
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteMessageTemplates([FromBody] int[] ids)
        {
            var userId = _userManager.GetUserId(User)!;
            var templates = await _context.MessageTemplates
                .Where(mt => ids.Contains(mt.Id) && mt.HostId == userId)
                .ToListAsync();

            if (templates.Any())
            {
                _context.MessageTemplates.RemoveRange(templates);
                await _context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = $"Đã xóa {templates.Count} tin nhắn mẫu thành công!" 
                });
            }

            return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu nào để xóa." });
        }

        [HttpPost]
        public async Task<IActionResult> BulkToggleMessageTemplates([FromBody] BulkToggleRequest request)
        {
            var userId = _userManager.GetUserId(User)!;
            var templates = await _context.MessageTemplates
                .Where(mt => request.Ids.Contains(mt.Id) && mt.HostId == userId)
                .ToListAsync();

            if (templates.Any())
            {
                foreach (var template in templates)
                {
                    template.IsActive = request.IsActive;
                    template.UpdatedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = $"Đã {(request.IsActive ? "kích hoạt" : "tạm dừng")} {templates.Count} tin nhắn mẫu!" 
                });
            }

            return Json(new { success = false, message = "Không tìm thấy tin nhắn mẫu nào để cập nhật." });
        }        // Message Template Statistics
        [HttpGet]
        public async Task<IActionResult> MessageTemplateStats()
        {
            var userId = _userManager.GetUserId(User)!;
            
            var templates = await _context.MessageTemplates
                .Where(mt => mt.HostId == userId)
                .ToListAsync();

            var stats = new MessageTemplateStatsViewModel
            {
                Total = templates.Count,
                Active = templates.Count(mt => mt.IsActive),
                Inactive = templates.Count(mt => !mt.IsActive),
                ByType = templates.GroupBy(mt => mt.Type)
                    .Select(g => new MessageTemplateTypeStats 
                    { 
                        Type = g.Key, 
                        Count = g.Count() 
                    })
                    .ToList()
            };

            return Json(new { success = true, stats });
        }

        // Export Message Templates
        [HttpGet]
        public async Task<IActionResult> ExportMessageTemplates()
        {
            var userId = _userManager.GetUserId(User)!;
            var templates = await _context.MessageTemplates
                .Where(mt => mt.HostId == userId)
                .OrderByDescending(mt => mt.CreatedAt)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Tên,Chủ đề,Nội dung,Loại,Trạng thái,Ngày tạo,Ngày cập nhật");
            
            foreach (var template in templates)
            {
                csv.AppendLine($"{template.Id}," +
                              $"\"{template.Name}\"," +
                              $"\"{template.Subject}\"," +
                              $"\"{template.Content.Replace("\"", "\"\"")}\"," +
                              $"{template.Type}," +
                              $"{(template.IsActive ? "Hoạt động" : "Tạm dừng")}," +
                              $"{template.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                              $"{template.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"message-templates-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ManageHomestay(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var homestay = await _homestayService.GetHomestayDetailAsync(id, userId);
            if (homestay == null)
                return NotFound();
            if (!homestay.Homestay.IsApproved)
                return View("NotApproved");
            return View("ManageHomestay", homestay);
        }
    }
}

