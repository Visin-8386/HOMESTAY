using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public class EntityFrameworkTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
        where TUser : class
    {
        private readonly ApplicationDbContext _context;

        public EntityFrameworkTokenProvider(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return await Task.FromResult(true);
        }

        public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var userId = await manager.GetUserIdAsync(user);
            var token = Guid.NewGuid().ToString();
            
            // Store token in database
            var existingToken = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && 
                                         t.LoginProvider == "EntityFramework" && 
                                         t.Name == purpose);
            
            if (existingToken != null)
            {
                existingToken.Value = token;
                _context.UserTokens.Update(existingToken);
            }
            else
            {
                var userToken = new IdentityUserToken<string>
                {
                    UserId = userId,
                    LoginProvider = "EntityFramework",
                    Name = purpose,
                    Value = token
                };
                _context.UserTokens.Add(userToken);
            }
            
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            var userId = await manager.GetUserIdAsync(user);
            
            var storedToken = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && 
                                         t.LoginProvider == "EntityFramework" && 
                                         t.Name == purpose);
            
            if (storedToken?.Value == token)
            {
                // Remove token after successful validation
                _context.UserTokens.Remove(storedToken);
                await _context.SaveChangesAsync();
                return true;
            }
            
            return false;
        }
    }
}
