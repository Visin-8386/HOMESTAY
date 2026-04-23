using WebHS.Models;
using System.Security.Claims;

namespace WebHS.Services
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user, IList<string> roles);
        ClaimsPrincipal? ValidateJwtToken(string token);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, string userId);
        Task StoreRefreshTokenAsync(string refreshToken, string userId, DateTime expiration);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
