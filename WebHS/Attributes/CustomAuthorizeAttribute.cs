using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebHS.Models;

namespace WebHS.Attributes
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _requiredRoles;

        public CustomAuthorizeAttribute(params string[] roles)
        {
            _requiredRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Nếu không đăng nhập
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Admin luôn có quyền truy cập tất cả
            if (user.IsInRole(UserRoles.Admin))
            {
                return; // Cho phép truy cập
            }

            // Kiểm tra role cụ thể
            if (_requiredRoles != null && _requiredRoles.Length > 0)
            {
                bool hasRequiredRole = _requiredRoles.Any(role => user.IsInRole(role));
                
                if (!hasRequiredRole)
                {
                    // Không có quyền truy cập - chuyển đến trang thông báo
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    return;
                }
            }
        }
    }
}