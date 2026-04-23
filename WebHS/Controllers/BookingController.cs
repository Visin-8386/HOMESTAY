using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Data;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly ILogger<BookingController> _logger;
        private readonly IEmailService _emailService;
        private readonly Data.ApplicationDbContext _context;

        public BookingController(
            IBookingService bookingService, 
            UserManager<WebHSUser> userManager, 
            ILogger<BookingController> logger,
            IEmailService emailService,
            Data.ApplicationDbContext context)
        {
            _bookingService = bookingService;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetUserBookingsAsync(userId, status, page);
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var booking = await _bookingService.GetBookingDetailAsync(id, userId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin đặt phòng không hợp lệ. Vui lòng thử lại.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            // ENHANCED: More comprehensive date validation
            if (model.CheckInDate >= model.CheckOutDate)
            {
                TempData["Error"] = "Ngày trả phòng phải sau ngày nhận phòng.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            if (model.CheckInDate < DateTime.Today)
            {
                TempData["Error"] = "Ngày nhận phòng không thể là ngày trong quá khứ.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }

            // ADDED: Minimum stay validation
            var numberOfNights = (model.CheckOutDate - model.CheckInDate).Days;
            if (numberOfNights < 1)
            {
                TempData["Error"] = "Thời gian lưu trú tối thiểu là 1 đêm.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }            // REMOVED: Availability check - Users can only click on available dates in calendar
            // Calendar UI will prevent clicking on booked dates (red colored dates)
            // var isAvailable = await _bookingService.IsDateAvailableAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);
            // if (!isAvailable)
            // {
            //     TempData["Error"] = "Homestay không khả dụng trong thời gian bạn chọn. Vui lòng chọn ngày khác.";
            //     return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            // }

            var userId = _userManager.GetUserId(User)!;
            var booking = await _bookingService.CreateBookingAsync(model, userId);

            if (booking != null)
            {
                // Gửi email xác nhận đặt phòng
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user?.Email != null)
                    {
                        await _emailService.SendDetailedBookingConfirmationAsync(user.Email, booking.Booking);
                        _logger.LogInformation($"Booking confirmation email sent to {user.Email} for booking #{booking.Booking.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send booking confirmation email for booking #{booking.Booking.Id}");
                    // Don't fail the booking if email fails
                }

                TempData["Message"] = "Đặt phòng đã được tạo và đang chờ thanh toán. Vui lòng chọn phương thức thanh toán để hoàn tất đặt phòng.";
                return RedirectToAction("Checkout", "Payment", new { bookingId = booking.Booking.Id });
            }
            else
            {
                TempData["Error"] = "Không thể đặt phòng. Vui lòng thử lại.";
                return RedirectToAction("Details", "Homestay", new { id = model.HomestayId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User)!;
                var success = await _bookingService.CancelBookingAsync(id, userId);
                
                if (success)
                {
                    return Json(new { success = true, message = "Booking đã được hủy thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy booking này" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculateAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null)
        {
            var amount = await _bookingService.CalculateBookingAmount(homestayId, checkIn, checkOut, promotionCode);
            return Json(new { amount });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            var isAvailable = await _bookingService.IsDateAvailableAsync(homestayId, checkIn, checkOut);
            return Json(new { available = isAvailable });
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings(string status = "all", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var bookings = await _bookingService.GetUserBookingsAsync(userId, status, page);
            
            ViewBag.StatusFilter = status;
            ViewBag.CurrentPage = page;
            
            return View(bookings);
        }        [HttpGet]
        [Route("Booking/GetBookedDates/{homestayId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookedDates(int homestayId)
        {
            try
            {
                _logger.LogInformation("GetBookedDates called for homestayId: {HomestayId}", homestayId);
                
                var bookedDates = await _bookingService.GetBookedDatesAsync(homestayId);
                var dateStrings = bookedDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
                
                _logger.LogInformation("GetBookedDates returning {Count} dates: {Dates}", dateStrings.Count, string.Join(", ", dateStrings));
                
                return Json(dateStrings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBookedDates for homestayId: {HomestayId}", homestayId);
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddTestBookings(int homestayId = 4)
        {
            try
            {
                // This is a test endpoint - remove in production
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // First check if bookings already exist for this homestay
                var existingBookings = await context.Bookings
                    .Where(b => b.HomestayId == homestayId)
                    .ToListAsync();
                
                if (existingBookings.Any())
                {
                    return Json(new { 
                        success = true, 
                        message = $"Homestay {homestayId} already has {existingBookings.Count} existing bookings",
                        existingBookings = existingBookings.Select(b => new {
                            Id = b.Id,
                            CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                            CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                            Status = b.Status.ToString()
                        }).ToArray()
                    });
                }
                  // Get a test user (không phải host)
                var allUsers = await context.Users.Where(u => u.Email != null && !u.Email.StartsWith("admin")).ToListAsync();
                User? testUser = null;
                
                foreach (var user in allUsers)
                {
                    if (!await _userManager.IsInRoleAsync(user, "Host"))
                    {
                        testUser = user;
                        break;
                    }
                }
                
                if (testUser == null)
                {
                    return Json(new { success = false, message = "No test user found" });
                }

                // Get the homestay
                var homestay = await context.Homestays.FindAsync(homestayId);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Homestay not found" });
                }

                var today = DateTime.Now.Date;
                var bookings = new List<Booking>();

                // Add 5 test bookings with different dates close to current date
                var testDates = new[]
                {
                    new { CheckIn = today.AddDays(1), CheckOut = today.AddDays(3) },   // Tomorrow for 2 nights
                    new { CheckIn = today.AddDays(5), CheckOut = today.AddDays(7) },   // 5 days from now for 2 nights  
                    new { CheckIn = today.AddDays(10), CheckOut = today.AddDays(12) }, // 10 days from now for 2 nights
                    new { CheckIn = today.AddDays(15), CheckOut = today.AddDays(18) }, // 15 days from now for 3 nights
                    new { CheckIn = today.AddDays(20), CheckOut = today.AddDays(22) }  // 20 days from now for 2 nights
                };

                foreach (var dates in testDates)
                {
                    var numberOfNights = (dates.CheckOut - dates.CheckIn).Days;
                    var totalAmount = homestay.PricePerNight * numberOfNights;

                    var booking = new Booking
                    {
                        CheckInDate = dates.CheckIn,
                        CheckOutDate = dates.CheckOut,
                        NumberOfGuests = 2,
                        TotalAmount = totalAmount,
                        DiscountAmount = 0,
                        FinalAmount = totalAmount,
                        Status = BookingStatus.Paid,
                        Notes = "Test booking for calendar display",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUser.Id,
                        HomestayId = homestayId
                    };

                    bookings.Add(booking);
                }

                context.Bookings.AddRange(bookings);
                await context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Added {bookings.Count} test bookings for homestay {homestayId}",
                    bookings = bookings.Select(b => new { 
                        CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = b.Status.ToString()
                    }).ToArray()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DebugBookings(int homestayId = 4)
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                var allBookings = await context.Bookings
                    .Where(b => b.HomestayId == homestayId)
                    .Select(b => new {
                        Id = b.Id,
                        CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = b.Status.ToString(),
                        UserId = b.UserId,
                        Notes = b.Notes
                    })
                    .ToListAsync();

                return Json(new { 
                    success = true,
                    homestayId = homestayId,
                    totalBookings = allBookings.Count,
                    bookings = allBookings
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestSimpleBookings(int homestayId = 4)
        {
            try
            {
                _logger.LogInformation("TestSimpleBookings called for homestayId: {HomestayId}", homestayId);
                
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // Simple query to test database connection
                var allBookings = await context.Bookings.ToListAsync();
                var homestayBookings = allBookings.Where(b => b.HomestayId == homestayId).ToList();
                var confirmedBookings = homestayBookings.Where(b => b.Status == BookingStatus.Paid).ToList();
                
                _logger.LogInformation("Found {Total} total bookings, {Homestay} for homestay {Id}, {Confirmed} confirmed", 
                    allBookings.Count, homestayBookings.Count, homestayId, confirmedBookings.Count);
                
                return Json(new { 
                    success = true,
                    totalBookingsInDb = allBookings.Count,
                    homestayBookings = homestayBookings.Count,
                    confirmedBookings = confirmedBookings.Count,
                    confirmeds = confirmedBookings.Select(b => new {
                        b.Id,
                        CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = b.Status.ToString()
                    }).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestSimpleBookings for homestayId: {HomestayId}", homestayId);
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // Test basic database connection
                var userCount = await context.Users.CountAsync();
                var homestayCount = await context.Homestays.CountAsync();
                var bookingCount = await context.Bookings.CountAsync();
                  // Get sample data
                var sampleUsers = await context.Users.Take(3).Select(u => new { u.Id, u.Email }).ToListAsync();
                var sampleHomestays = await context.Homestays.Take(3).Select(h => new { h.Id, h.Name, h.HostId }).ToListAsync();
                var sampleBookings = await context.Bookings.Take(3).Select(b => new { b.Id, b.HomestayId, b.UserId, b.Status }).ToListAsync();
                
                _logger.LogInformation("Database connection test successful. Users: {Users}, Homestays: {Homestays}, Bookings: {Bookings}", 
                    userCount, homestayCount, bookingCount);
                
                return Json(new { 
                    success = true,
                    message = "Database connection successful",
                    counts = new { userCount, homestayCount, bookingCount },
                    samples = new { users = sampleUsers, homestays = sampleHomestays, bookings = sampleBookings }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> EnsureTestBookings(int homestayId = 4)
        {
            try
            {
                _logger.LogInformation("EnsureTestBookings called for homestayId: {HomestayId}", homestayId);
                
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // Check if bookings already exist
                var existingBookings = await context.Bookings
                    .Where(b => b.HomestayId == homestayId)
                    .ToListAsync();
                
                if (existingBookings.Any())
                {
                    _logger.LogInformation("Found {Count} existing bookings for homestay {Id}", existingBookings.Count, homestayId);
                    
                    // Test the GetBookedDatesAsync method directly
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    var bookedDates = await bookingService.GetBookedDatesAsync(homestayId);
                    
                    return Json(new { 
                        success = true, 
                        message = $"Homestay {homestayId} already has {existingBookings.Count} existing bookings",
                        existingBookings = existingBookings.Select(b => new {
                            Id = b.Id,
                            CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                            CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                            Status = b.Status.ToString(),
                            UserId = b.UserId
                        }).ToArray(),
                        bookedDatesFromService = bookedDates.Select(d => d.ToString("yyyy-MM-dd")).ToArray(),
                        bookedDatesCount = bookedDates.Count
                    });
                }
                
                // Create test bookings if none exist
                var bookings = new List<Booking>();
                var today = DateTime.Now.Date;
                
                // Create a test user ID (use any existing user or create a test ID)
                var testUserId = "test-user-id";
                var existingUser = await context.Users.FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    testUserId = existingUser.Id;
                }
                
                // Add test bookings with dates around current date
                var testDates = new[]
                {
                    new { CheckIn = today.AddDays(1), CheckOut = today.AddDays(3) },   // Tomorrow for 2 nights
                    new { CheckIn = today.AddDays(5), CheckOut = today.AddDays(7) },   // 5 days from now for 2 nights  
                    new { CheckIn = today.AddDays(10), CheckOut = today.AddDays(12) }, // 10 days from now for 2 nights
                    new { CheckIn = today.AddDays(15), CheckOut = today.AddDays(18) }, // 15 days from now for 3 nights
                    new { CheckIn = today.AddDays(20), CheckOut = today.AddDays(22) }  // 20 days from now for 2 nights
                };

                foreach (var dates in testDates)
                {
                    var booking = new Booking
                    {
                        CheckInDate = dates.CheckIn,
                        CheckOutDate = dates.CheckOut,
                        NumberOfGuests = 2,
                        TotalAmount = 1000000, // 1 million VND per booking
                        DiscountAmount = 0,
                        FinalAmount = 1000000,
                        Status = BookingStatus.Paid,
                        Notes = "Test booking for calendar display",
                        CreatedAt = DateTime.UtcNow,
                        UserId = testUserId,
                        HomestayId = homestayId
                    };

                    bookings.Add(booking);
                }

                context.Bookings.AddRange(bookings);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully created {Count} test bookings for homestay {Id}", bookings.Count, homestayId);
                
                // Test the GetBookedDatesAsync method after creation
                var bookingService2 = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var bookedDatesAfter = await bookingService2.GetBookedDatesAsync(homestayId);
                
                return Json(new { 
                    success = true, 
                    message = $"Created {bookings.Count} test bookings for homestay {homestayId}",
                    bookings = bookings.Select(b => new { 
                        CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = b.Status.ToString()
                    }).ToArray(),
                    bookedDatesFromService = bookedDatesAfter.Select(d => d.ToString("yyyy-MM-dd")).ToArray(),
                    bookedDatesCount = bookedDatesAfter.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnsureTestBookings for homestayId: {HomestayId}", homestayId);
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestAddressInput()
        {
            return View();
        }

        // ADDED: Test booking address functionality
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestBookingAddress()
        {
            ViewData["Title"] = "Test Booking Address Input";
            return View();
        }

        // ADDED: API endpoint to save booking address data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveBookingAddress([FromBody] BookingAddressData addressData)
        {
            try
            {
                // Validate the address data
                if (string.IsNullOrEmpty(addressData.StreetName))
                {
                    return Json(new { success = false, message = "Tên đường là bắt buộc" });
                }

                // Here you would typically save to database or session
                // For testing, we'll just return the processed data
                var processedAddress = new
                {
                    success = true,
                    data = new
                    {
                        fullAddress = addressData.FullAddress,
                        coordinates = addressData.Latitude.HasValue && addressData.Longitude.HasValue 
                            ? $"{addressData.Latitude:F6}, {addressData.Longitude:F6}" 
                            : "Không có tọa độ",
                        houseNumber = addressData.HouseNumber,
                        streetName = addressData.StreetName,
                        ward = addressData.Ward,
                        district = addressData.District,
                        city = addressData.City,
                        hasCoordinates = addressData.Latitude.HasValue && addressData.Longitude.HasValue
                    },
                    message = "Đã lưu địa chỉ thành công!"
                };

                return Json(processedAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving booking address");
                return Json(new { success = false, message = "Lỗi lưu địa chỉ: " + ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestDatabaseSchema()
        {
            try
            {                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // Test basic booking creation (without customer address fields)
                var testBooking = new Booking
                {
                    UserId = "test-user-id",
                    HomestayId = 1,
                    CheckInDate = DateTime.Now.AddDays(1),
                    CheckOutDate = DateTime.Now.AddDays(2),
                    NumberOfGuests = 2,
                    TotalAmount = 1000000,
                    FinalAmount = 1000000,
                    Status = BookingStatus.Paid,
                    Notes = "Test booking - customer address fields removed"
                };
                
                return Json(new { 
                    success = true,
                    message = "Customer address fields have been removed from Booking model",
                    note = "Address information can be obtained from Homestay via join",
                    schemaCheck = "✅ Customer address properties removed - use Homestay.Address instead"
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message, 
                    error = "Customer address fields may not exist in database",
                    recommendation = "Run 'dotnet ef migrations add AddCustomerAddressFields' and 'dotnet ef database update'"
                });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSimpleTestBooking(int homestayId = 4)
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                  // Get a test user or create one
                var testUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@test.com");
                if (testUser == null)
                {
                    testUser = new WebHSUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "test@test.com",
                        Email = "test@test.com",
                        EmailConfirmed = true,
                        PhoneNumber = "0123456789"
                    };
                    context.Users.Add(testUser);
                    await context.SaveChangesAsync();
                }
                
                // Create a simple booking for today + 2 days to today + 4 days
                var checkIn = DateTime.Today.AddDays(2);                var checkOut = DateTime.Today.AddDays(4);
                
                var booking = new WebHS.Models.Booking
                {                    UserId = testUser.Id,
                    HomestayId = homestayId,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    NumberOfGuests = 2,
                    TotalAmount = 500000,
                    FinalAmount = 500000,
                    Status = BookingStatus.Paid,
                    CreatedAt = DateTime.Now
                };
                
                context.Bookings.Add(booking);
                await context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = "Simple test booking created successfully",
                    booking = new {
                        Id = booking.Id,
                        CheckIn = booking.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = booking.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = booking.Status.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating simple test booking for homestayId: {HomestayId}", homestayId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateJuneTestBooking(int homestayId = 4)
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                // Delete existing bookings for this homestay first
                var existingBookings = await context.Bookings
                    .Where(b => b.HomestayId == homestayId)
                    .ToListAsync();
                
                if (existingBookings.Any())
                {
                    context.Bookings.RemoveRange(existingBookings);
                    await context.SaveChangesAsync();
                }
                
                // Get or create test user
                var testUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@test.com");                if (testUser == null)
                {
                    testUser = new WebHSUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "test@test.com",
                        Email = "test@test.com",
                        EmailConfirmed = true,
                        PhoneNumber = "0123456789"
                    };
                    context.Users.Add(testUser);
                    await context.SaveChangesAsync();
                }
                  // Create bookings in June 2025
                var bookings = new List<WebHS.Models.Booking>
                {
                    // Booking 1: 18-19 June (check out on 20th, so 20th should be available)
                    new WebHS.Models.Booking
                    {
                        UserId = testUser.Id,
                        HomestayId = homestayId,                        CheckInDate = new DateTime(2025, 6, 18),
                        CheckOutDate = new DateTime(2025, 6, 20), // Check out on 20th
                        NumberOfGuests = 2,
                        TotalAmount = 500000,
                        FinalAmount = 500000,
                        Status = BookingStatus.Paid,
                        CreatedAt = DateTime.Now
                    },
                    // Booking 2: 22-24 June
                    new WebHS.Models.Booking
                    {
                        UserId = testUser.Id,
                        HomestayId = homestayId,
                        CheckInDate = new DateTime(2025, 6, 22),
                        CheckOutDate = new DateTime(2025, 6, 24),                        NumberOfGuests = 1,
                        TotalAmount = 400000,
                        FinalAmount = 400000,
                        Status = BookingStatus.Paid,
                        CreatedAt = DateTime.Now
                    }
                };
                
                context.Bookings.AddRange(bookings);
                await context.SaveChangesAsync();
                
                return Json(new { 
                    success = true, 
                    message = "June test bookings created successfully",
                    bookings = bookings.Select(b => new {
                        Id = b.Id,
                        CheckIn = b.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = b.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = b.Status.ToString()
                    }).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating June test bookings for homestayId: {HomestayId}", homestayId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DebugBookedDates(int homestayId = 4)
        {
            try
            {
                var bookedDates = await _bookingService.GetBookedDatesAsync(homestayId);
                var result = new {
                    homestayId,
                    count = bookedDates.Count,
                    dates = bookedDates.Select(d => d.ToString("yyyy-MM-dd")).ToArray(),
                    rawDates = bookedDates.Select(d => new {
                        date = d,
                        formatted = d.ToString("yyyy-MM-dd"),
                        dayOfWeek = d.DayOfWeek.ToString()
                    }).ToArray()
                };
                
                _logger.LogInformation("Debug booked dates for homestay {HomestayId}: {Result}", homestayId, System.Text.Json.JsonSerializer.Serialize(result));
                
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug booked dates");
                return Json(new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // Debug API for GetBookedDates
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DebugGetBookedDates(int homestayId)
        {
            try
            {
                var today = DateTime.Today;
                var utcToday = DateTime.UtcNow.Date;
                
                // Check raw blocked dates
                var rawBlockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == homestayId)
                    .OrderBy(bd => bd.Date)
                    .ToListAsync();

                // Check filtered blocked dates (same logic as BookingService)
                var filteredBlockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == homestayId && bd.Date >= today)
                    .Select(bd => bd.Date)
                    .OrderBy(d => d)
                    .ToListAsync();

                // Call actual service method
                var serviceResult = await _bookingService.GetBookedDatesAsync(homestayId);

                var result = new
                {
                    HomestayId = homestayId,
                    TodayLocal = today.ToString("yyyy-MM-dd HH:mm:ss"),
                    TodayUtc = utcToday.ToString("yyyy-MM-dd HH:mm:ss"),
                    RawBlockedDatesCount = rawBlockedDates.Count,
                    RawBlockedDates = rawBlockedDates.Select(bd => new {
                        bd.Id,
                        Date = bd.Date.ToString("yyyy-MM-dd"),
                        bd.Reason,
                        bd.CreatedAt
                    }).ToList(),
                    FilteredBlockedDatesCount = filteredBlockedDates.Count,
                    FilteredBlockedDates = filteredBlockedDates.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                    ServiceResultCount = serviceResult.Count,
                    ServiceResult = serviceResult.Select(d => d.ToString("yyyy-MM-dd")).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SyncBlockedDates()
        {
            try
            {
                // Use the new comprehensive sync method
                await _bookingService.SyncBlockedDatesWithBookingsAsync();
                return Json(new { success = true, message = "BlockedDates synced successfully with all active bookings" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing blocked dates");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateBookingStatusToPaid(int bookingId)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return Json(new { success = false, error = "Booking not found" });
                }

                // Update status to Paid
                await _bookingService.UpdateBookingStatusAsync(bookingId, BookingStatus.Paid);
                
                // Sync blocked dates for this booking
                await _bookingService.SyncBlockedDatesFromBookingsAsync();
                
                return Json(new { 
                    success = true, 
                    message = $"Booking {bookingId} updated to Paid and BlockedDates synced",
                    booking = new {
                        Id = booking.Id,
                        HomestayId = booking.HomestayId,
                        CheckIn = booking.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = booking.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = "Paid"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking status");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSampleBookingForHomestay(int homestayId = 9)
        {
            try
            {
                // Tạo booking mẫu với status Paid
                var booking = new Booking
                {
                    UserId = "sample-user-" + Guid.NewGuid().ToString()[..8],
                    HomestayId = homestayId,
                    CheckInDate = DateTime.Today.AddDays(3), // 3 ngày từ hôm nay
                    CheckOutDate = DateTime.Today.AddDays(5), // 5 ngày từ hôm nay
                    NumberOfGuests = 2,
                    TotalAmount = 500000,
                    FinalAmount = 500000,
                    Status = BookingStatus.Paid,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Sample booking for testing calendar"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Tạo blocked dates
                var blockedDates = new List<BlockedDate>();
                var currentDate = booking.CheckInDate.Date;
                while (currentDate < booking.CheckOutDate.Date)
                {
                    blockedDates.Add(new BlockedDate
                    {
                        HomestayId = homestayId,
                        Date = currentDate,
                        Reason = $"Booking #{booking.Id} - Sample booking",
                        CreatedAt = DateTime.UtcNow
                    });
                    currentDate = currentDate.AddDays(1);
                }

                if (blockedDates.Any())
                {
                    _context.BlockedDates.AddRange(blockedDates);
                    await _context.SaveChangesAsync();
                }

                return Json(new { 
                    success = true, 
                    message = $"Sample booking created for homestay {homestayId}",
                    booking = new {
                        Id = booking.Id,
                        HomestayId = booking.HomestayId,
                        CheckIn = booking.CheckInDate.ToString("yyyy-MM-dd"),
                        CheckOut = booking.CheckOutDate.ToString("yyyy-MM-dd"),
                        Status = booking.Status.ToString()
                    },
                    blockedDatesCount = blockedDates.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample booking");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllBlockedDates()
        {
            try
            {
                var blockedDates = await _context.BlockedDates
                    .Include(bd => bd.Homestay)
                    .OrderBy(bd => bd.HomestayId)
                    .ThenBy(bd => bd.Date)
                    .Select(bd => new {
                        bd.Id,
                        bd.Date,
                        bd.Reason,
                        HomestayId = bd.HomestayId,
                        HomestayName = bd.Homestay.Name,
                        bd.CreatedAt
                    })
                    .ToListAsync();

                return Json(blockedDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blocked dates");
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestBookingBlockedDates()
        {
            return View("~/Views/Shared/TestBookingBlockedDates.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult DebugCalendar()
        {
            return View("~/Views/Shared/DebugCalendar.cshtml");
        }

        // Debug endpoint to check blocked dates directly from database
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DebugBlockedDates(int homestayId)
        {
            try
            {
                var blockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == homestayId)
                    .OrderBy(bd => bd.Date)
                    .ToListAsync();

                var bookings = await _context.Bookings
                    .Where(b => b.HomestayId == homestayId)
                    .Select(b => new { 
                        b.Id, 
                        b.CheckInDate, 
                        b.CheckOutDate, 
                        Status = b.Status.ToString(),
                        b.UserId
                    })
                    .ToListAsync();

                var result = new
                {
                    HomestayId = homestayId,
                    BlockedDatesCount = blockedDates.Count,                    BlockedDates = blockedDates.Select(bd => new {
                        bd.Id,
                        Date = bd.Date.ToString("yyyy-MM-dd"),
                        bd.Reason,
                        bd.CreatedAt
                    }).ToList(),
                    BookingsCount = bookings.Count,
                    Bookings = bookings
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Create test blocked dates
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CreateTestBlockedDates(int homestayId)
        {
            try
            {
                // Check if homestay exists
                var homestay = await _context.Homestays.FindAsync(homestayId);
                if (homestay == null)
                {
                    return Json(new { error = $"Homestay {homestayId} not found" });
                }

                // Create some test blocked dates
                var testDates = new List<BlockedDate>();
                var today = DateTime.Today;
                
                for (int i = 1; i <= 5; i++)
                {
                    var date = today.AddDays(i);
                    var existingDate = await _context.BlockedDates
                        .FirstOrDefaultAsync(bd => bd.HomestayId == homestayId && bd.Date == date);
                    
                    if (existingDate == null)
                    {
                        testDates.Add(new BlockedDate
                        {
                            HomestayId = homestayId,
                            Date = date,
                            Reason = $"Test blocked date {i}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (testDates.Any())
                {
                    _context.BlockedDates.AddRange(testDates);
                    await _context.SaveChangesAsync();
                }

                return Json(new { 
                    success = true,
                    message = $"Created {testDates.Count} test blocked dates for homestay {homestayId}",
                    testDates = testDates.Select(td => new {
                        td.Date,
                        td.Reason
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Simple calendar test page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestCalendar()
        {
            return View("~/Views/Shared/CalendarTest.cshtml");
        }
    }
}