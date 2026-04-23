using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebHS.Models;

namespace WebHS.Services
{
    public class CustomSignInManager : SignInManager<User>
    {
        public CustomSignInManager(
            UserManager<User> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<User> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<User>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<User> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var user = await UserManager.FindByNameAsync(userName) ?? await UserManager.FindByEmailAsync(userName);
            
            if (user != null && !user.IsActive)
            {
                Logger.LogWarning("User {UserId} attempted to sign in while account is inactive", user.Id);
                return SignInResult.NotAllowed;
            }

            return await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
        }

        public override async Task<SignInResult> PasswordSignInAsync(User user, string password, bool isPersistent, bool lockoutOnFailure)
        {
            if (user == null)
            {
                return await base.PasswordSignInAsync(user!, password, isPersistent, lockoutOnFailure);
            }

            if (!user.IsActive)
            {
                Logger.LogWarning("User {UserId} attempted to sign in while account is inactive", user.Id);
                return SignInResult.NotAllowed;
            }

            return await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        }

        public override async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor)
        {
            var user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
            
            if (user != null && !user.IsActive)
            {
                Logger.LogWarning("User {UserId} attempted to sign in via external provider while account is inactive", user.Id);
                return SignInResult.NotAllowed;
            }

            return await base.ExternalLoginSignInAsync(loginProvider, providerKey, isPersistent, bypassTwoFactor);
        }

        public override async Task<SignInResult> CheckPasswordSignInAsync(User user, string password, bool lockoutOnFailure)
        {
            if (user == null)
            {
                return await base.CheckPasswordSignInAsync(user!, password, lockoutOnFailure);
            }

            if (!user.IsActive)
            {
                Logger.LogWarning("User {UserId} attempted to sign in while account is inactive", user.Id);
                return SignInResult.NotAllowed;
            }

            return await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        }
    }
}
