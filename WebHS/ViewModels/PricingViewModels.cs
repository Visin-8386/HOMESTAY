using WebHS.Models;

namespace WebHS.ViewModels
{
    public class PricingManagementViewModel
    {
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public decimal DefaultPricePerNight { get; set; }
        public List<HomestayPricing> PricingRules { get; set; } = new();
    }

    public class PricingCalendarViewModel
    {
        public int HomestayId { get; set; }
        public string HomestayName { get; set; } = string.Empty;
        public decimal DefaultPricePerNight { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public List<HomestayPricing> PricingRules { get; set; } = new();
        public List<DateTime> BlockedDates { get; set; } = new();
        public List<DateTime> BookedDates { get; set; } = new();

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public DateTime FirstDayOfMonth => new DateTime(Year, Month, 1);
        public DateTime LastDayOfMonth => FirstDayOfMonth.AddMonths(1).AddDays(-1);
        public int DaysInMonth => DateTime.DaysInMonth(Year, Month);
        public DayOfWeek FirstDayOfWeek => FirstDayOfMonth.DayOfWeek;

        public decimal GetPriceForDate(DateTime date)
        {
            var customPricing = PricingRules.FirstOrDefault(pr => pr.Date.Date == date.Date);
            return customPricing?.PricePerNight ?? DefaultPricePerNight;
        }

        public bool IsDateBlocked(DateTime date)
        {
            return BlockedDates.Any(bd => bd.Date == date.Date);
        }

        public bool IsDateBooked(DateTime date)
        {
            return BookedDates.Any(bd => bd.Date == date.Date);
        }

        public bool HasCustomPrice(DateTime date)
        {
            return PricingRules.Any(pr => pr.Date.Date == date.Date);
        }
    }

    public class BulkPricingViewModel
    {
        public int HomestayId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public string? Note { get; set; }
    }

    public class DayPricingViewModel
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string? Note { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsBooked { get; set; }
        public bool HasCustomPrice { get; set; }
        public bool IsPastDate { get; set; }
    }
}
