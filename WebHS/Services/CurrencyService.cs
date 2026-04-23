using System.Text.Json;

namespace WebHS.Services
{
    public interface ICurrencyService
    {
        Task<decimal> ConvertVNDToUSDAsync(decimal vndAmount);
        decimal ConvertVNDToUSD(decimal vndAmount, decimal exchangeRate = 24000); // Default rate
        Task<decimal> GetCurrentExchangeRateAsync();
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyService> _logger;
        private readonly IConfiguration _configuration;
        private static decimal _cachedExchangeRate = 24000; // Default VND to USD rate
        private static DateTime _cacheExpiry = DateTime.MinValue;

        public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<decimal> ConvertVNDToUSDAsync(decimal vndAmount)
        {
            try
            {
                var exchangeRate = await GetCurrentExchangeRateAsync();
                return Math.Round(vndAmount / exchangeRate, 2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current exchange rate, using default rate");
                return ConvertVNDToUSD(vndAmount);
            }
        }

        public decimal ConvertVNDToUSD(decimal vndAmount, decimal exchangeRate = 24000)
        {
            return Math.Round(vndAmount / exchangeRate, 2);
        }

        public async Task<decimal> GetCurrentExchangeRateAsync()
        {
            // Use cached rate if still valid (cache for 1 hour)
            if (_cacheExpiry > DateTime.UtcNow)
            {
                return _cachedExchangeRate;
            }

            try
            {
                // Using a free exchange rate API
                var response = await _httpClient.GetStringAsync("https://api.exchangerate-api.com/v4/latest/USD");
                var json = JsonDocument.Parse(response);
                
                if (json.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("VND", out var vndRate))
                {
                    _cachedExchangeRate = vndRate.GetDecimal();
                    _cacheExpiry = DateTime.UtcNow.AddHours(1);
                    _logger.LogInformation("Updated exchange rate: 1 USD = {Rate} VND", _cachedExchangeRate);
                    return _cachedExchangeRate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch exchange rate from API");
            }

            // Fallback to default rate
            return 24000;
        }
    }
}
