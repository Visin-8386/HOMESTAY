using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public class PricingService : IPricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<HomestayPricing>> GetPricingRulesAsync(int homestayId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.HomestayPricings
                .Where(hp => hp.HomestayId == homestayId);

            if (startDate.HasValue)
            {
                query = query.Where(hp => hp.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(hp => hp.Date <= endDate.Value.Date);
            }

            return await query
                .OrderBy(hp => hp.Date)
                .ToListAsync();
        }

        public async Task<HomestayPricing?> GetPricingRuleAsync(int homestayId, DateTime date)
        {
            return await _context.HomestayPricings
                .FirstOrDefaultAsync(hp => hp.HomestayId == homestayId && hp.Date.Date == date.Date);
        }

        public async Task<decimal> GetPriceForDateAsync(int homestayId, DateTime date)
        {
            var customPricing = await GetPricingRuleAsync(homestayId, date);
            if (customPricing != null)
            {
                return customPricing.PricePerNight;
            }

            // Fallback to default homestay price
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == homestayId);

            return homestay?.PricePerNight ?? 0;
        }

        public async Task<bool> SetPricingRuleAsync(int homestayId, DateTime date, decimal price, string? note = null)
        {
            try
            {
                var existingRule = await GetPricingRuleAsync(homestayId, date);

                if (existingRule != null)
                {
                    existingRule.PricePerNight = price;
                    existingRule.Note = note;
                    existingRule.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newRule = new HomestayPricing
                    {
                        HomestayId = homestayId,
                        Date = date.Date,
                        PricePerNight = price,
                        Note = note
                    };
                    _context.HomestayPricings.Add(newRule);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePricingRuleAsync(int homestayId, DateTime date)
        {
            try
            {
                var rule = await GetPricingRuleAsync(homestayId, date);
                if (rule != null)
                {
                    _context.HomestayPricings.Remove(rule);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetBulkPricingAsync(int homestayId, DateTime startDate, DateTime endDate, decimal price, string? note = null)
        {
            try
            {
                var dates = GetDateRange(startDate, endDate);
                
                foreach (var date in dates)
                {
                    await SetPricingRuleAsync(homestayId, date, price, note);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> CalculateTotalPriceAsync(int homestayId, DateTime checkInDate, DateTime checkOutDate)
        {
            var dates = GetDateRange(checkInDate, checkOutDate.AddDays(-1)); // Exclude checkout date
            decimal totalPrice = 0;

            foreach (var date in dates)
            {
                var priceForDate = await GetPriceForDateAsync(homestayId, date);
                totalPrice += priceForDate;
            }

            return totalPrice;
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
