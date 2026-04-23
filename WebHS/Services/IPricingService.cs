using WebHS.Models;

namespace WebHS.Services
{
    public interface IPricingService
    {
        Task<List<HomestayPricing>> GetPricingRulesAsync(int homestayId, DateTime? startDate = null, DateTime? endDate = null);
        Task<HomestayPricing?> GetPricingRuleAsync(int homestayId, DateTime date);
        Task<decimal> GetPriceForDateAsync(int homestayId, DateTime date);
        Task<bool> SetPricingRuleAsync(int homestayId, DateTime date, decimal price, string? note = null);
        Task<bool> RemovePricingRuleAsync(int homestayId, DateTime date);
        Task<bool> SetBulkPricingAsync(int homestayId, DateTime startDate, DateTime endDate, decimal price, string? note = null);
        Task<decimal> CalculateTotalPriceAsync(int homestayId, DateTime checkInDate, DateTime checkOutDate);
    }
}
