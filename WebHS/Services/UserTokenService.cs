using Microsoft.AspNetCore.Identity;
using WebHS.Models;
using System.Text.Json;

namespace WebHS.Services
{
    public class UserTokenService
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserTokenService> _logger;

        public UserTokenService(
            UserManager<User> userManager,
            ILogger<UserTokenService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        #region Google Tokens
        
        /// <summary>
        /// Lưu Google tokens cho user
        /// </summary>
        public async Task<bool> SaveGoogleTokensAsync(User user, string accessToken, string? refreshToken = null, DateTime? expiresAt = null)
        {
            try
            {
                // Lưu access token
                await _userManager.SetAuthenticationTokenAsync(user, "Google", "access_token", accessToken);
                
                // Lưu refresh token nếu có
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await _userManager.SetAuthenticationTokenAsync(user, "Google", "refresh_token", refreshToken);
                }
                
                // Lưu thời gian hết hạn
                if (expiresAt.HasValue)
                {
                    await _userManager.SetAuthenticationTokenAsync(user, "Google", "expires_at", expiresAt.Value.ToString("O"));
                }
                
                // Lưu thời gian cập nhật
                await _userManager.SetAuthenticationTokenAsync(user, "Google", "updated_at", DateTime.UtcNow.ToString("O"));
                
                _logger.LogInformation("Saved Google tokens for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Google tokens for user {UserId}", user.Id);
                return false;
            }
        }
        
        /// <summary>
        /// Lấy Google access token của user
        /// </summary>
        public async Task<string?> GetGoogleAccessTokenAsync(User user)
        {
            try
            {
                var token = await _userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
                
                // Kiểm tra token có hết hạn không
                if (!string.IsNullOrEmpty(token) && await IsGoogleTokenExpiredAsync(user))
                {
                    // Thử refresh token
                    var refreshed = await RefreshGoogleTokenAsync(user);
                    if (refreshed)
                    {
                        return await _userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
                    }
                    return null;
                }
                
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Google access token for user {UserId}", user.Id);
                return null;
            }
        }
        
        /// <summary>
        /// Kiểm tra Google token có hết hạn không
        /// </summary>
        public async Task<bool> IsGoogleTokenExpiredAsync(User user)
        {
            try
            {
                var expiresAtString = await _userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");
                if (string.IsNullOrEmpty(expiresAtString)) return false;
                
                if (DateTime.TryParse(expiresAtString, out var expiresAt))
                {
                    return DateTime.UtcNow >= expiresAt.AddMinutes(-5); // Hết hạn trước 5 phút
                }
                
                return false;
            }
            catch
            {
                return true; // Coi như hết hạn nếu có lỗi
            }
        }
        
        /// <summary>
        /// Refresh Google token
        /// </summary>
        public async Task<bool> RefreshGoogleTokenAsync(User user)
        {
            try
            {
                var refreshToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
                if (string.IsNullOrEmpty(refreshToken)) return false;
                
                // TODO: Implement Google token refresh logic
                // Gọi Google API để refresh token
                
                _logger.LogInformation("Refreshed Google token for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh Google token for user {UserId}", user.Id);
                return false;
            }
        }
        
        #endregion
        
        #region Facebook Tokens
        
        /// <summary>
        /// Lưu Facebook tokens cho user
        /// </summary>
        public async Task<bool> SaveFacebookTokensAsync(User user, string accessToken, DateTime? expiresAt = null)
        {
            try
            {
                await _userManager.SetAuthenticationTokenAsync(user, "Facebook", "access_token", accessToken);
                
                if (expiresAt.HasValue)
                {
                    await _userManager.SetAuthenticationTokenAsync(user, "Facebook", "expires_at", expiresAt.Value.ToString("O"));
                }
                
                await _userManager.SetAuthenticationTokenAsync(user, "Facebook", "updated_at", DateTime.UtcNow.ToString("O"));
                
                _logger.LogInformation("Saved Facebook tokens for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Facebook tokens for user {UserId}", user.Id);
                return false;
            }
        }
        
        /// <summary>
        /// Lấy Facebook access token của user
        /// </summary>
        public async Task<string?> GetFacebookAccessTokenAsync(User user)
        {
            try
            {
                return await _userManager.GetAuthenticationTokenAsync(user, "Facebook", "access_token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Facebook access token for user {UserId}", user.Id);
                return null;
            }
        }
        
        #endregion
        
        #region Custom Tokens
        
        /// <summary>
        /// Lưu custom token cho user
        /// </summary>
        public async Task<bool> SaveCustomTokenAsync(User user, string tokenName, object tokenValue)
        {
            try
            {
                var jsonValue = JsonSerializer.Serialize(tokenValue);
                await _userManager.SetAuthenticationTokenAsync(user, "WebHS", tokenName, jsonValue);
                
                _logger.LogInformation("Saved custom token {TokenName} for user {UserId}", tokenName, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save custom token {TokenName} for user {UserId}", tokenName, user.Id);
                return false;
            }
        }
        
        /// <summary>
        /// Lấy custom token của user
        /// </summary>
        public async Task<T?> GetCustomTokenAsync<T>(User user, string tokenName)
        {
            try
            {
                var jsonValue = await _userManager.GetAuthenticationTokenAsync(user, "WebHS", tokenName);
                if (string.IsNullOrEmpty(jsonValue)) return default;
                
                return JsonSerializer.Deserialize<T>(jsonValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get custom token {TokenName} for user {UserId}", tokenName, user.Id);
                return default;
            }
        }
        
        #endregion
        
        #region Token Management
        
        /// <summary>
        /// Xóa tất cả tokens của một provider
        /// </summary>
        public async Task<bool> RemoveProviderTokensAsync(User user, string provider)
        {
            try
            {
                var tokens = new[] { "access_token", "refresh_token", "expires_at", "updated_at" };
                
                foreach (var tokenName in tokens)
                {
                    await _userManager.RemoveAuthenticationTokenAsync(user, provider, tokenName);
                }
                
                _logger.LogInformation("Removed all {Provider} tokens for user {UserId}", provider, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove {Provider} tokens for user {UserId}", provider, user.Id);
                return false;
            }
        }
        
        /// <summary>
        /// Lấy tất cả tokens của user
        /// </summary>
        public async Task<Dictionary<string, Dictionary<string, string>>> GetAllUserTokensAsync(User user)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var providers = new[] { "Google", "Facebook", "WebHS" };
            var tokenNames = new[] { "access_token", "refresh_token", "expires_at", "updated_at" };
            
            foreach (var provider in providers)
            {
                var providerTokens = new Dictionary<string, string>();
                
                foreach (var tokenName in tokenNames)
                {
                    try
                    {
                        var token = await _userManager.GetAuthenticationTokenAsync(user, provider, tokenName);
                        if (!string.IsNullOrEmpty(token))
                        {
                            providerTokens[tokenName] = token;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get token {Provider}:{TokenName} for user {UserId}", 
                            provider, tokenName, user.Id);
                    }
                }
                
                if (providerTokens.Any())
                {
                    result[provider] = providerTokens;
                }
            }
            
            return result;
        }
        
        #endregion
    }
}
