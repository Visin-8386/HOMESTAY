using Microsoft.AspNetCore.Mvc;
using WebHS.Services;
using WebHS.Models;

namespace WebHS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomestayApiController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IHomestayService _homestayService;
        private readonly IPricingService _pricingService;

        public HomestayApiController(IBookingService bookingService, IHomestayService homestayService, IPricingService pricingService)
        {
            _bookingService = bookingService;
            _homestayService = homestayService;
            _pricingService = pricingService;
        }

        [HttpGet("{id}/availability")]
        public async Task<IActionResult> CheckAvailability(int id, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            try
            {
                if (checkIn >= checkOut || checkIn < DateTime.Today)
                {
                    return Ok(new { 
                        available = false, 
                        message = "Ngày nhận phòng và trả phòng không hợp lệ." 
                    });
                }

                var isAvailable = await _bookingService.IsDateAvailableAsync(id, checkIn, checkOut);
                
                return Ok(new { 
                    available = isAvailable,
                    message = isAvailable ? "Phòng có sẵn" : "Phòng đã được đặt trong khoảng thời gian này"
                });
            }
            catch (Exception)
            {
                return Ok(new { 
                    available = false, 
                    message = "Có lỗi xảy ra khi kiểm tra tình trạng phòng." 
                });
            }
        }

        [HttpGet("{id}/price")]
        public async Task<IActionResult> CalculatePrice(int id, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut, [FromQuery] string? promotionCode = null)
        {
            try
            {
                if (checkIn >= checkOut || checkIn < DateTime.Today)
                {
                    return Ok(new { 
                        success = false,
                        message = "Ngày nhận phòng và trả phòng không hợp lệ." 
                    });
                }

                var homestay = await _homestayService.GetHomestayByIdAsync(id);
                if (homestay == null || !homestay.IsActive || !homestay.IsApproved)
                {
                    return Ok(new { 
                        success = false,
                        message = "Homestay không tồn tại hoặc không khả dụng." 
                    });
                }

                var numberOfNights = (checkOut - checkIn).Days;
                
                // Use PricingService to calculate subtotal with dynamic pricing
                var subTotal = await _pricingService.CalculateTotalPriceAsync(id, checkIn, checkOut);
                var totalAmount = await _bookingService.CalculateBookingAmount(id, checkIn, checkOut, promotionCode);
                var discountAmount = subTotal - totalAmount;

                return Ok(new { 
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
                return Ok(new { 
                    success = false,
                    message = "Có lỗi xảy ra khi tính toán giá." 
                });
            }
        }

        [HttpGet("{id}/daily-pricing")]
        public async Task<IActionResult> GetDailyPricing(int id, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
        {
            try
            {
                if (checkIn >= checkOut || checkIn < DateTime.Today)
                {
                    return Ok(new { 
                        success = false,
                        message = "Ngày nhận phòng và trả phòng không hợp lệ." 
                    });
                }

                var homestay = await _homestayService.GetHomestayByIdAsync(id);
                if (homestay == null || !homestay.IsActive || !homestay.IsApproved)
                {
                    return Ok(new { 
                        success = false,
                        message = "Homestay không tồn tại hoặc không khả dụng." 
                    });
                }

                var numberOfNights = (checkOut - checkIn).Days;
                var dailyPrices = new List<object>();
                decimal totalPrice = 0;

                for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
                {
                    var priceForDate = await _pricingService.GetPriceForDateAsync(id, date);
                    dailyPrices.Add(new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        price = priceForDate
                    });
                    totalPrice += priceForDate;
                }

                return Ok(new { 
                    success = true,
                    numberOfNights = numberOfNights,
                    dailyPrices = dailyPrices,
                    averagePricePerNight = numberOfNights > 0 ? totalPrice / numberOfNights : homestay.PricePerNight,
                    totalPrice = totalPrice
                });
            }
            catch (Exception)
            {
                return Ok(new { 
                    success = false,
                    message = "Có lỗi xảy ra khi tính toán giá." 
                });
            }
        }
    }
}
