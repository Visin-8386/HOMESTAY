using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUserRoles = WebHS.Models.UserRoles;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    public class HomestayController : Controller
    {
        private readonly IHomestayService _homestayService;
        private readonly IBookingService _bookingService;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IPricingService _pricingService;

        // Fixed constructor - changed WebHSUser back to User to match field declaration
        public HomestayController(IHomestayService homestayService, IBookingService bookingService, UserManager<User> userManager, ApplicationDbContext context, IPricingService pricingService)
        {
            _homestayService = homestayService;
            _bookingService = bookingService;
            _userManager = userManager;
            _context = context;
            _pricingService = pricingService;
        }

        public async Task<IActionResult> Index(HomestaySearchViewModel searchModel)
        {
            var result = await _homestayService.SearchHomestaysAsync(searchModel);
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Chỉ lấy homestay theo id, không truyền userId để tránh filter quá chặt
            var homestay = await _homestayService.GetHomestayDetailAsync(id, null);

            if (homestay == null)
                return NotFound();

            // Nếu chưa được duyệt thì chuyển sang trang thông báo
            if (!homestay.Homestay.IsApproved)
                return View("NotApproved");

            return View(homestay);
        }

        [HttpGet]
        [Authorize(Roles = WebHS.Models.UserRoles.Host)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
            
            return View();
        }

        [HttpPost]
        [Authorize(Roles = WebHSUserRoles.Host)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateHomestayViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User)!;
                var homestayId = await _homestayService.CreateHomestayAsync(model, userId);

                TempData["Message"] = "Homestay đã được tạo thành công và đang chờ duyệt!";
                return RedirectToAction("Dashboard", "Host");
            }

            // Reload amenities for the form
            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = WebHSUserRoles.Host)]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var homestay = await _homestayService.GetHomestayByIdAsync(id);

            if (homestay == null || homestay.HostId != userId)
                return NotFound();
            
            // Fixed: Added missing code after the NotFound check
            var model = new EditHomestayViewModel
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
                ExistingImages = homestay.Images,
                AmenityIds = homestay.HomestayAmenities.Select(ha => ha.AmenityId).ToArray()
            };

            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = WebHSUserRoles.Host)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditHomestayViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User)!;
                var result = await _homestayService.UpdateHomestayAsync(model, userId);

                if (result)
                {
                    TempData["Message"] = "Homestay đã được cập nhật thành công!";
                    return RedirectToAction("Dashboard", "Host");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không thể cập nhật homestay.");
                }
            }

            // Reload amenities for the form
            ViewBag.Amenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = WebHSUserRoles.Host)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var result = await _homestayService.DeleteHomestayAsync(id, userId);

            if (result)
            {
                TempData["Message"] = "Homestay đã được xóa thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa homestay.";
            }

            return RedirectToAction("Dashboard", "Host");
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                if (checkIn >= checkOut || checkIn < DateTime.Today)
                {
                    return Json(new { 
                        available = false, 
                        message = "Ngày nhận phòng và trả phòng không hợp lệ." 
                    });
                }

                var isAvailable = await _bookingService.IsDateAvailableAsync(homestayId, checkIn, checkOut);
                
                return Json(new { 
                    available = isAvailable,
                    message = isAvailable ? "Phòng có sẵn" : "Phòng đã được đặt trong khoảng thời gian này"
                });
            }
            catch (Exception)
            {
                return Json(new { 
                    available = false, 
                    message = "Có lỗi xảy ra khi kiểm tra tình trạng phòng." 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculatePrice(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null)
        {
            try
            {
                if (checkIn >= checkOut || checkIn < DateTime.Today)
                {
                    return Json(new { 
                        success = false,
                        message = "Ngày nhận phòng và trả phòng không hợp lệ." 
                    });
                }

                var homestay = await _homestayService.GetHomestayByIdAsync(homestayId);
                if (homestay == null || !homestay.IsActive || !homestay.IsApproved)
                {
                    return Json(new { 
                        success = false,
                        message = "Homestay không tồn tại hoặc không khả dụng." 
                    });
                }

                var numberOfNights = (checkOut - checkIn).Days;
                
                // Use PricingService to calculate subtotal with dynamic pricing
                var subTotal = await _pricingService.CalculateTotalPriceAsync(homestayId, checkIn, checkOut);
                var totalAmount = await _bookingService.CalculateBookingAmount(homestayId, checkIn, checkOut, promotionCode);
                var discountAmount = subTotal - totalAmount;

                return Json(new { 
                    success = true,
                    numberOfNights = numberOfNights,
                    pricePerNight = numberOfNights > 0 ? subTotal / numberOfNights : homestay.PricePerNight, // Average price per night
                    subTotal = subTotal,
                    discountAmount = discountAmount,
                    totalAmount = totalAmount,
                    promotionApplied = !string.IsNullOrEmpty(promotionCode) && discountAmount > 0
                });
            }
            catch (Exception)
            {
                return Json(new { 
                    success = false,
                    message = "Có lỗi xảy ra khi tính toán giá." 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GeocodeAddress(string address, string city, string state)
        {
            try
            {
                var (latitude, longitude) = await _homestayService.GeocodeAddressAsync(address, city, state);
                
                if (latitude.HasValue && longitude.HasValue)
                {
                    return Json(new { 
                        success = true,
                        latitude = latitude.Value,
                        longitude = longitude.Value
                    });
                }

                return Json(new { 
                    success = false,
                    message = "Không thể xác định tọa độ cho địa chỉ này" 
                });
            }
            catch (Exception)
            {
                return Json(new { 
                    success = false,
                    message = "Có lỗi xảy ra khi xác định tọa độ" 
                });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAvailablePromotions(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive && 
                               p.StartDate <= DateTime.Now && 
                               p.EndDate >= DateTime.Now)
                    .Select(p => new {
                        code = p.Code,
                        name = p.Name,
                        description = p.Description,
                        type = p.Type.ToString(),
                        value = p.Value,
                        minAmount = p.MinOrderAmount,
                        maxDiscount = p.MaxDiscountAmount
                    })
                    .ToListAsync();

                return Json(promotions);
            }
            catch
            {
                return Json(new { error = "Không thể tải danh sách khuyến mãi" });
            }
        }
    }
}