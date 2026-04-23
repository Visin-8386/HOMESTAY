using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebHS.Models;

namespace WebHS.Controllers
{
    public class ClaimsTestController : Controller
    {
        // Trang chủ kiểm tra claims
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Policy = "can_manage_users")]
        public IActionResult ManageUsers()
        {
            return View();
        }

        [Authorize(Policy = "can_manage_properties")]
        public IActionResult ManageProperties()
        {
            return View();
        }

        [Authorize(Policy = "can_moderate_reviews")]
        public IActionResult ModerateReviews()
        {
            return View();
        }

        [Authorize(Policy = "super_host")]
        public IActionResult SuperHostOnly()
        {
            return View();
        }

        [Authorize]
        public IActionResult UserClaims()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            List<object> claims;

            if (claimsIdentity?.Claims != null)
            {
                claims = claimsIdentity.Claims
                    .Select(c => new { Type = c.Type ?? string.Empty, Value = c.Value ?? string.Empty })
                    .ToList<object>();
            }
            else
            {
                claims = new List<object>();
            }
            
            return View(claims);
        }
    }
}
