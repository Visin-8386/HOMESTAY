using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebHS.Models;

namespace WebHS.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        
        public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync();
                    
                    // Chạy mỗi 24 giờ
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Token Cleanup Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during token cleanup");
                    // Chờ 1 giờ trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task CleanupExpiredTokensAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TokenCleanupService>>();
            
            try
            {
                var users = await userManager.Users.ToListAsync();
                int expiredTokensCount = 0;
                int totalCheckedTokens = 0;

                foreach (var user in users)
                {
                    // Kiểm tra Google tokens
                    var googleResult = await CleanupUserTokensAsync(userManager, user, "Google");
                    expiredTokensCount += googleResult.ExpiredCount;
                    totalCheckedTokens += googleResult.CheckedCount;
                    
                    // Kiểm tra Facebook tokens
                    var facebookResult = await CleanupUserTokensAsync(userManager, user, "Facebook");
                    expiredTokensCount += facebookResult.ExpiredCount;
                    totalCheckedTokens += facebookResult.CheckedCount;
                }

                logger.LogInformation("Token cleanup completed. Checked: {TotalChecked}, Expired: {ExpiredCount}", 
                    totalCheckedTokens, expiredTokensCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during token cleanup process");
            }
        }

        private async Task<(int ExpiredCount, int CheckedCount)> CleanupUserTokensAsync(UserManager<User> userManager, User user, string provider)
        {
            int expiredCount = 0;
            int checkedCount = 0;
            
            try
            {
                // Lấy access token và expires_at
                var accessToken = await userManager.GetAuthenticationTokenAsync(user, provider, "access_token");
                var expiresAt = await userManager.GetAuthenticationTokenAsync(user, provider, "expires_at");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    checkedCount++;
                    
                    // Kiểm tra token có hết hạn không
                    if (IsTokenExpired(expiresAt))
                    {
                        // Xóa tất cả tokens của provider này cho user
                        await RemoveAllTokensForProviderAsync(userManager, user, provider);
                        expiredCount++;
                        
                        _logger.LogInformation("Removed expired {Provider} tokens for user {UserId}", 
                            provider, user.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up {Provider} tokens for user {UserId}", 
                    provider, user.Id);
            }
            
            return (expiredCount, checkedCount);
        }

        private bool IsTokenExpired(string? expiresAtString)
        {
            if (string.IsNullOrEmpty(expiresAtString))
            {
                // Nếu không có expires_at thì coi như token cũ, nên xóa
                return true;
            }

            try
            {
                // Thử parse theo format Unix timestamp
                if (long.TryParse(expiresAtString, out var unixTimestamp))
                {
                    var expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                    return expiresAt <= DateTimeOffset.UtcNow.AddMinutes(-5); // Buffer 5 phút
                }

                // Thử parse theo format DateTime
                if (DateTime.TryParse(expiresAtString, out var dateTime))
                {
                    return dateTime <= DateTime.UtcNow.AddMinutes(-5);
                }

                // Thử parse theo format DateTimeOffset
                if (DateTimeOffset.TryParse(expiresAtString, out var dateTimeOffset))
                {
                    return dateTimeOffset <= DateTimeOffset.UtcNow.AddMinutes(-5);
                }

                // Không parse được thì coi như hết hạn
                return true;
            }
            catch
            {
                return true;
            }
        }

        private async Task RemoveAllTokensForProviderAsync(UserManager<User> userManager, User user, string provider)
        {
            var tokenNames = new[] { "access_token", "refresh_token", "expires_at", "token_type", "id_token" };
            
            foreach (var tokenName in tokenNames)
            {
                try
                {
                    await userManager.RemoveAuthenticationTokenAsync(user, provider, tokenName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error removing {TokenName} for {Provider} from user {UserId}", 
                        tokenName, provider, user.Id);
                }
            }
        }
    }
}
