using WebHS.Models;

namespace WebHS.Services
{
    public class MockPayPalService : IPayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<MockPayPalService> _logger;

        public MockPayPalService(
            IConfiguration configuration, 
            ICurrencyService currencyService,
            ILogger<MockPayPalService> logger)
        {
            _configuration = configuration;
            _currencyService = currencyService;
            _logger = logger;
        }

        public async Task<string> CreatePaymentAsync(Payment payment, Booking booking)
        {
            try
            {
                // Convert VND to USD for display
                var usdAmount = await _currencyService.ConvertVNDToUSDAsync(payment.Amount);
                
                // Generate a mock PayPal approval URL
                var returnUrl = _configuration["PayPal:ReturnUrl"] ?? string.Empty;
                var cancelUrl = _configuration["PayPal:CancelUrl"] ?? string.Empty;
                
                // Create a mock PayPal token
                var mockToken = $"EC-{Guid.NewGuid().ToString("N")[..15].ToUpper()}";
                
                // Store the mock token as transaction ID
                payment.TransactionId = mockToken;
                
                _logger.LogInformation($"Mock PayPal payment created: Amount={usdAmount:C} USD (from {payment.Amount:C} VND), Token={mockToken}");
                
                // Create a mock PayPal approval URL that will redirect back to our success page
                var approvalUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={mockToken}&useraction=commit" +
                                 $"&returnUrl={Uri.EscapeDataString(returnUrl)}" +
                                 $"&cancelUrl={Uri.EscapeDataString(cancelUrl)}" +
                                 $"&amount={usdAmount:F2}";
                
                // For demo purposes, directly redirect to success
                await Task.Delay(1000); // Simulate API call delay
                
                return $"{returnUrl}&token={mockToken}&PayerID=MOCKPAYERID123";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mock PayPal payment");
                throw new Exception($"Mock PayPal payment creation failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> CapturePaymentAsync(string paymentId, string payerId)
        {
            try
            {
                // Simulate PayPal capture API call
                await Task.Delay(500);
                
                _logger.LogInformation($"Mock PayPal payment captured: PaymentId={paymentId}, PayerId={payerId}");
                
                // Always return success for mock
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing mock PayPal payment: {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> ValidatePaymentAsync(string paymentId)
        {
            try
            {
                // Simulate PayPal validation API call
                await Task.Delay(300);
                
                _logger.LogInformation($"Mock PayPal payment validated: PaymentId={paymentId}");
                
                // Always return success for mock
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating mock PayPal payment: {PaymentId}", paymentId);
                return false;
            }
        }
    }
}
