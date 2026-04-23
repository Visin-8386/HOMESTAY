using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHS.ViewModels;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    [Authorize]
    public class PricingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;

        public PricingController(ApplicationDbContext context, UserManager<WebHSUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Pricing/Manage/5
        public async Task<IActionResult> Manage(int id)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .Include(h => h.PricingRules)
                .FirstOrDefaultAsync(h => h.Id == id && h.HostId == userId);

            if (homestay == null)
            {
                return NotFound();
            }

            var viewModel = new PricingManagementViewModel
            {
                HomestayId = homestay.Id,
                HomestayName = homestay.Name,
                DefaultPricePerNight = homestay.PricePerNight,
                PricingRules = homestay.PricingRules.ToList()
            };

            return View(viewModel);
        }

        // GET: /Pricing/Calendar/5
        public async Task<IActionResult> Calendar(int id, int year = 0, int month = 0)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .Include(h => h.PricingRules)
                .Include(h => h.BlockedDates)
                .Include(h => h.Bookings)
                .FirstOrDefaultAsync(h => h.Id == id && h.HostId == userId);

            if (homestay == null)
            {
                return NotFound();
            }

            var currentDate = DateTime.Now;
            var targetDate = year > 0 && month > 0 ? new DateTime(year, month, 1) : currentDate;

            var viewModel = new PricingCalendarViewModel
            {
                HomestayId = homestay.Id,
                HomestayName = homestay.Name,
                DefaultPricePerNight = homestay.PricePerNight,
                Year = targetDate.Year,
                Month = targetDate.Month,
                PricingRules = homestay.PricingRules
                    .Where(pr => pr.Date.Year == targetDate.Year && pr.Date.Month == targetDate.Month)
                    .ToList(),
                BlockedDates = homestay.BlockedDates
                    .Where(bd => bd.Date.Year == targetDate.Year && bd.Date.Month == targetDate.Month)
                    .Select(bd => bd.Date)
                    .ToList(),
                BookedDates = homestay.Bookings
                    .Where(b => b.Status == BookingStatus.Paid)
                    .SelectMany(b => GetDateRange(b.CheckInDate, b.CheckOutDate))
                    .Where(date => date.Year == targetDate.Year && date.Month == targetDate.Month)
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: /Pricing/SetPrice
        [HttpPost]
        public async Task<IActionResult> SetPrice(int homestayId, DateTime date, decimal price, string? note = null)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId && h.HostId == userId);

            if (homestay == null)
            {
                return Json(new { success = false, message = "Homestay not found" });
            }

            if (date < DateTime.Today)
            {
                return Json(new { success = false, message = "Cannot set prices for past dates" });
            }

            if (price < 0)
            {
                return Json(new { success = false, message = "Price cannot be negative" });
            }

            try
            {
                var existingPricing = await _context.HomestayPricings
                    .FirstOrDefaultAsync(hp => hp.HomestayId == homestayId && hp.Date.Date == date.Date);

                if (existingPricing != null)
                {
                    existingPricing.PricePerNight = price;
                    existingPricing.Note = note;
                    existingPricing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newPricing = new HomestayPricing
                    {
                        HomestayId = homestayId,
                        Date = date.Date,
                        PricePerNight = price,
                        Note = note
                    };
                    _context.HomestayPricings.Add(newPricing);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Price updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating price: " + ex.Message });
            }
        }

        // POST: /Pricing/RemovePrice
        [HttpPost]
        public async Task<IActionResult> RemovePrice(int homestayId, DateTime date)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId && h.HostId == userId);

            if (homestay == null)
            {
                return Json(new { success = false, message = "Homestay not found" });
            }

            try
            {
                var pricing = await _context.HomestayPricings
                    .FirstOrDefaultAsync(hp => hp.HomestayId == homestayId && hp.Date.Date == date.Date);

                if (pricing != null)
                {
                    _context.HomestayPricings.Remove(pricing);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Custom price removed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing price: " + ex.Message });
            }
        }

        // POST: /Pricing/SetBulkPrice
        [HttpPost]
        public async Task<IActionResult> SetBulkPrice(int homestayId, DateTime startDate, DateTime endDate, decimal price, string? note = null)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId && h.HostId == userId);

            if (homestay == null)
            {
                return Json(new { success = false, message = "Homestay not found" });
            }

            if (startDate < DateTime.Today || endDate < DateTime.Today)
            {
                return Json(new { success = false, message = "Cannot set prices for past dates" });
            }

            if (startDate > endDate)
            {
                return Json(new { success = false, message = "Start date must be before end date" });
            }

            if (price < 0)
            {
                return Json(new { success = false, message = "Price cannot be negative" });
            }

            try
            {
                var dates = GetDateRange(startDate, endDate);
                var affectedCount = 0;

                foreach (var date in dates)
                {
                    var existingPricing = await _context.HomestayPricings
                        .FirstOrDefaultAsync(hp => hp.HomestayId == homestayId && hp.Date.Date == date.Date);

                    if (existingPricing != null)
                    {
                        existingPricing.PricePerNight = price;
                        existingPricing.Note = note;
                        existingPricing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var newPricing = new HomestayPricing
                        {
                            HomestayId = homestayId,
                            Date = date.Date,
                            PricePerNight = price,
                            Note = note
                        };
                        _context.HomestayPricings.Add(newPricing);
                    }
                    affectedCount++;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Prices updated for {affectedCount} dates successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating prices: " + ex.Message });
            }
        }

        // GET: /Pricing/GetPrice
        [HttpGet]
        public async Task<IActionResult> GetPrice(int homestayId, DateTime date)
        {
            var pricing = await _context.HomestayPricings
                .FirstOrDefaultAsync(hp => hp.HomestayId == homestayId && hp.Date.Date == date.Date);

            if (pricing != null)
            {
                return Json(new 
                { 
                    hasCustomPrice = true, 
                    price = pricing.PricePerNight, 
                    note = pricing.Note 
                });
            }

            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId);

            return Json(new 
            { 
                hasCustomPrice = false, 
                price = homestay?.PricePerNight ?? 0, 
                note = (string?)null 
            });
        }

        // GET: /Pricing/CreateTestData/5 - TEST METHOD ONLY
        [HttpGet]
        public async Task<IActionResult> CreateTestData(int id)
        {
            try
            {
                var homestay = await _context.Homestays.FirstOrDefaultAsync(h => h.Id == id);
                if (homestay == null)
                {
                    return Json(new { success = false, message = "Homestay not found" });
                }

                // Clear existing pricing data for this homestay
                var existingPricing = await _context.HomestayPricings
                    .Where(hp => hp.HomestayId == id)
                    .ToListAsync();
                _context.HomestayPricings.RemoveRange(existingPricing);

                // Create test pricing data for the next 30 days
                var startDate = DateTime.Today;
                var basePrice = homestay.PricePerNight;
                var pricingData = new List<HomestayPricing>();

                for (int i = 0; i < 30; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayOfWeek = date.DayOfWeek;
                    decimal price;
                    string note;

                    // Weekend pricing (Friday and Saturday)
                    if (dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday)
                    {
                        price = basePrice * 1.5m; // 50% premium
                        note = "Weekend premium pricing";
                    }
                    // Quiet days (Sunday and Monday)
                    else if (dayOfWeek == DayOfWeek.Sunday || dayOfWeek == DayOfWeek.Monday)
                    {
                        price = basePrice * 0.8m; // 20% discount
                        note = "Weekday discount";
                    }
                    // Regular days
                    else
                    {
                        price = basePrice; // Regular price
                        note = "Regular pricing";
                    }

                    pricingData.Add(new HomestayPricing
                    {
                        HomestayId = id,
                        Date = date,
                        PricePerNight = price,
                        Note = note,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                _context.HomestayPricings.AddRange(pricingData);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Created {pricingData.Count} test pricing records",
                    data = pricingData.Select(p => new {
                        date = p.Date.ToString("yyyy-MM-dd"),
                        price = p.PricePerNight,
                        note = p.Note,
                        dayOfWeek = p.Date.DayOfWeek.ToString()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating test data: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockDate(int homestayId, DateTime date, string? reason = null)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId && h.HostId == userId);

            if (homestay == null)
                return Json(new { success = false, message = "Homestay not found" });

            if (date < DateTime.Today)
                return Json(new { success = false, message = "Cannot block past dates" });

            if (_context.BlockedDates.Any(bd => bd.HomestayId == homestayId && bd.Date == date.Date))
                return Json(new { success = false, message = "Date already blocked" });

            _context.BlockedDates.Add(new BlockedDate
            {
                HomestayId = homestayId,
                Date = date.Date,
                Reason = reason
            });
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Date blocked successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockDate(int homestayId, DateTime date)
        {
            var userId = _userManager.GetUserId(User);
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId && h.HostId == userId);

            if (homestay == null)
                return Json(new { success = false, message = "Homestay not found" });

            var blocked = await _context.BlockedDates.FirstOrDefaultAsync(bd => bd.HomestayId == homestayId && bd.Date == date.Date);
            if (blocked == null)
                return Json(new { success = false, message = "Date is not blocked" });

            _context.BlockedDates.Remove(blocked);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Date unblocked successfully" });
        }

        private static IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                yield return date;
            }
        }
    }
}
