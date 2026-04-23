using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Models;

namespace WebHS.ViewComponents
{
    public class EmailConfirmationNotificationViewComponent : ViewComponent
    {
        private readonly UserManager<User> _userManager;

        public EmailConfirmationNotificationViewComponent(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null)
            {
                return Content(string.Empty);
            }

            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (isEmailConfirmed)
            {
                return Content(string.Empty);
            }

            return View(user);
        }
    }
}
