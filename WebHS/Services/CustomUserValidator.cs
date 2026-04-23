using Microsoft.AspNetCore.Identity;
using WebHS.Models;

namespace WebHS.Services
{
    public class CustomUserValidator : IUserValidator<User>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
        {
            if (user != null && !user.IsActive)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "UserInactive",
                    Description = "Tài khoản của bạn đã bị khóa."
                }));
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}
