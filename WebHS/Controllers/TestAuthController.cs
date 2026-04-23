using Microsoft.AspNetCore.Mvc;
using WebHS.Attributes;
using WebHS.Models;

namespace WebHS.Controllers
{
    public class TestAuthController : Controller
    {
        // Trang này chỉ Admin mới vào được (nhưng Admin luôn vào được tất cả)
        [CustomAuthorize(UserRoles.Admin)]
        public IActionResult AdminOnly()
        {
            ViewData["Title"] = "Trang chỉ dành cho Admin";
            ViewData["Message"] = "Chào mừng Admin! Bạn có quyền truy cập trang này.";
            return View("TestPage");
        }

        // Trang này chỉ Host mới vào được (Admin cũng vào được)
        [CustomAuthorize(UserRoles.Host)]
        public IActionResult HostOnly()
        {
            ViewData["Title"] = "Trang chỉ dành cho Host";
            ViewData["Message"] = "Chào mừng Host! Bạn có quyền truy cập trang này.";
            return View("TestPage");
        }

        // Trang này chỉ User thường mới vào được (Admin cũng vào được)
        [CustomAuthorize(UserRoles.User)]
        public IActionResult UserOnly()
        {
            ViewData["Title"] = "Trang chỉ dành cho User";
            ViewData["Message"] = "Chào mừng User! Bạn có quyền truy cập trang này.";
            return View("TestPage");
        }

        // Trang này Host hoặc User đều vào được (Admin luôn vào được)
        [CustomAuthorize(UserRoles.Host, UserRoles.User)]
        public IActionResult HostOrUser()
        {
            ViewData["Title"] = "Trang dành cho Host hoặc User";
            ViewData["Message"] = "Chào mừng! Bạn là Host hoặc User và có quyền truy cập trang này.";
            return View("TestPage");
        }

        // Trang public - ai cũng vào được
        public IActionResult Public()
        {
            ViewData["Title"] = "Trang công khai";
            ViewData["Message"] = "Đây là trang công khai, ai cũng có thể truy cập.";
            return View("TestPage");
        }
    }
}
