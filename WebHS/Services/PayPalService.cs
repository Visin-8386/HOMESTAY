using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using System.Collections.Generic;
using WebHS.Models;

namespace WebHS.Services
{
    public interface IPayPalService
    {
        Task<string> CreatePaymentAsync(Payment payment, Booking booking);
        Task<bool> CapturePaymentAsync(string paymentId, string payerId);
        Task<bool> ValidatePaymentAsync(string paymentId);
    }

    public class PayPalService : IPayPalService
    {
        private readonly PayPalEnvironment _environment;
        private readonly PayPalHttpClient _client;
        private readonly IConfiguration _configuration;
        private readonly ICurrencyService _currencyService;        public PayPalService(IConfiguration configuration, ICurrencyService currencyService)
        {
            _configuration = configuration;
            _currencyService = currencyService;
            var clientId = _configuration["PaymentSettings:PayPal:ClientId"];
            var clientSecret = _configuration["PaymentSettings:PayPal:ClientSecret"];
            var mode = _configuration["PaymentSettings:PayPal:Mode"];

            // Debug logging để kiểm tra configuration
            Console.WriteLine($"PayPal ClientId: {clientId}");
            Console.WriteLine($"PayPal Mode: {mode}");
            Console.WriteLine($"PayPal ClientSecret exists: {!string.IsNullOrEmpty(clientSecret)}");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentException("PayPal ClientId and ClientSecret must be configured");
            }

            // Use sandbox or live environment based on configuration
            _environment = mode?.ToLower() == "live" 
                ? new LiveEnvironment(clientId, clientSecret)
                : new SandboxEnvironment(clientId, clientSecret);
            
            _client = new PayPalHttpClient(_environment);
        }public async Task<string> CreatePaymentAsync(Payment payment, Booking booking)
        {
            try
            {
                // Convert VND to USD for PayPal
                var usdAmount = await _currencyService.ConvertVNDToUSDAsync(payment.Amount);
                
                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(BuildRequestBody(payment, booking, usdAmount));

                var response = await _client.Execute(request);
                var order = response.Result<Order>();

                // Find the approval URL
                var approvalUrl = order.Links.First(link => link.Rel == "approve").Href;
                
                // Store the PayPal order ID as transaction ID
                payment.TransactionId = order.Id;
                
                return approvalUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"PayPal payment creation failed: {ex.Message}", ex);
            }
        }        public async Task<bool> CapturePaymentAsync(string paymentId, string payerId)
        {
            try
            {
                Console.WriteLine($"PayPal CapturePaymentAsync called - PaymentId: {paymentId}, PayerId: {payerId}");
                
                var request = new OrdersCaptureRequest(paymentId);
                request.RequestBody(new OrderActionRequest());
                
                Console.WriteLine($"Executing PayPal capture request for order: {paymentId}");
                var response = await _client.Execute(request);
                var order = response.Result<Order>();

                Console.WriteLine($"PayPal capture response - Status: {order.Status}");
                var isCompleted = order.Status == "COMPLETED";
                Console.WriteLine($"PayPal capture result: {isCompleted}");
                
                return isCompleted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing PayPal payment {paymentId}: {ex.Message}");
                Console.WriteLine($"PayPal capture exception stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> ValidatePaymentAsync(string paymentId)
        {
            try
            {
                var request = new OrdersGetRequest(paymentId);                var response = await _client.Execute(request);
                var order = response.Result<Order>();
                
                return order.Status == "COMPLETED" || order.Status == "APPROVED";
            }
            catch (Exception ex)
            {
                // Log error - intentionally using the exception variable
                Console.WriteLine($"Error validating PayPal payment {paymentId}: {ex.Message}");
                return false;
            }
        }        private OrderRequest BuildRequestBody(Payment payment, Booking booking, decimal usdAmount)
        {
            var returnUrl = _configuration["PaymentSettings:PayPal:ReturnUrl"];
            var cancelUrl = _configuration["PaymentSettings:PayPal:CancelUrl"];

            return new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        ReferenceId = booking.Id.ToString(),
                        Description = $"Booking for {booking.Homestay?.Name}",
                        CustomId = payment.Id.ToString(),
                        SoftDescriptor = "WebHS Payment",
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = "USD",
                            Value = usdAmount.ToString("F2")
                        }
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    BrandName = "WebHS Homestay",
                    LandingPage = "BILLING",
                    ShippingPreference = "NO_SHIPPING",
                    UserAction = "PAY_NOW"
                }
            };
        }
    }
}
