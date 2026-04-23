using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebHS.Models;

namespace WebHS.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public TokenService(IConfiguration configuration, ICacheService cacheService)
        {
            _configuration = configuration;
            _cacheService = cacheService;
        }        public string GenerateJwtToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("IsHost", roles.Contains("Host").ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:SecretKey"] ?? "WebHS_Super_Secret_Key_2024!@#$%^&*"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "WebHS",
                audience: _configuration["Jwt:Audience"] ?? "WebHS.Users",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateJwtToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration["Jwt:SecretKey"] ?? "WebHS_Super_Secret_Key_2024!@#$%^&*"));

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "WebHS",
                    ValidAudience = _configuration["Jwt:Audience"] ?? "WebHS.Users",
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, string userId)
        {
            var cacheKey = $"refresh_token_{userId}_{refreshToken}";
            var cachedToken = await _cacheService.GetAsync<string>(cacheKey);
            return cachedToken != null;
        }

        public async Task StoreRefreshTokenAsync(string refreshToken, string userId, DateTime expiration)
        {
            var cacheKey = $"refresh_token_{userId}_{refreshToken}";
            var timeUntilExpiration = expiration - DateTime.UtcNow;
            await _cacheService.SetAsync(cacheKey, refreshToken, timeUntilExpiration);
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            await _cacheService.RemoveByPatternAsync($"refresh_token_.*_{refreshToken}");
        }
    }
}
