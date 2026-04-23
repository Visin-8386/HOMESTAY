using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebHS.Data;
using WebHS.Models;
using WebHS.Services;
using WebHS.ViewModels;

namespace WebHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClaimsManagementController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IClaimManagementService _claimService;
        private readonly ApplicationDbContext _context;

        public ClaimsManagementController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IClaimManagementService claimService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _claimService = claimService;
            _context = context;
        }

        // Hiển thị trang quản lý claims
        public IActionResult Index()
        {
            return View();
        }

        #region User Claims

        // Hiển thị claims của một user
        public async Task<IActionResult> UserClaims(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var userClaims = await _claimService.GetUserClaimsAsync(user);

            var model = new UserClaimsViewModel
            {
                UserId = userId ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Claims = userClaims.Select(c => new ClaimViewModel
                {
                    ClaimType = c.Type ?? string.Empty,
                    ClaimValue = c.Value ?? string.Empty
                }).ToList()
            };

            return View(model);
        }

        // Hiển thị form thêm claim cho user
        [HttpGet]
        public IActionResult AddUserClaim(string userId)
        {
            var model = new ClaimViewModel
            {
                UserId = userId
            };
            return View(model);
        }

        // Xử lý thêm claim cho user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserClaim(ClaimViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.UserId))
            {
                ModelState.AddModelError("", "User ID là bắt buộc.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            await _claimService.AddUserClaimAsync(user, model.ClaimType, model.ClaimValue);
            
            return RedirectToAction(nameof(UserClaims), new { userId = model.UserId });
        }

        // Xử lý xóa claim của user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserClaim(string userId, string claimType, string claimValue)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            await _claimService.RemoveUserClaimAsync(user, claimType, claimValue);

            return RedirectToAction(nameof(UserClaims), new { userId });
        }

        // API để cấp quyền nhanh cho user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickGrantClaim(string userId, string claimType, string claimValue = "true")
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(claimType))
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra user đã có claim này chưa
                var existingClaims = await _userManager.GetClaimsAsync(user);
                var existingClaim = existingClaims.FirstOrDefault(c => c.Type == claimType);

                if (existingClaim != null)
                {
                    // Nếu đã có claim này, cập nhật giá trị
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                    await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
                    
                    return Json(new { 
                        success = true, 
                        message = $"Đã cập nhật quyền '{GetClaimDisplayName(claimType)}' cho {user.FirstName} {user.LastName}." 
                    });
                }
                else
                {
                    // Thêm claim mới
                    await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
                    
                    return Json(new { 
                        success = true, 
                        message = $"Đã cấp quyền '{GetClaimDisplayName(claimType)}' cho {user.FirstName} {user.LastName}." 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // API để thu hồi quyền nhanh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickRevokeClaim(string userId, string claimType)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(claimType))
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var existingClaims = await _userManager.GetClaimsAsync(user);
                var existingClaim = existingClaims.FirstOrDefault(c => c.Type == claimType);

                if (existingClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                    
                    return Json(new { 
                        success = true, 
                        message = $"Đã thu hồi quyền '{GetClaimDisplayName(claimType)}' của {user.FirstName} {user.LastName}." 
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "Người dùng không có quyền này để thu hồi." 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // API để lấy danh sách quyền đặc biệt của user
        [HttpGet]
        public async Task<IActionResult> GetUserSpecialClaims(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User ID không hợp lệ." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                var userClaims = await _userManager.GetClaimsAsync(user);
                
                // Lọc các quyền đặc biệt (loại bỏ các claim mặc định)
                var specialClaimTypes = new[]
                {
                    WebHSClaimTypes.SuperHost,
                    WebHSClaimTypes.CanViewReports,
                    WebHSClaimTypes.CanModerateReviews,
                    WebHSClaimTypes.CanManagePromotions,
                    WebHSClaimTypes.CanManageUsers,
                    WebHSClaimTypes.CanManageProperties,
                    WebHSClaimTypes.CanApproveListings,
                    WebHSClaimTypes.CanAccessApiKeys,
                    WebHSClaimTypes.CanManageRoles
                };

                var specialClaims = userClaims
                    .Where(c => specialClaimTypes.Contains(c.Type) && c.Value == "true")
                    .Select(c => new
                    {
                        type = c.Type,
                        value = c.Value,
                        displayName = GetClaimDisplayName(c.Type)
                    })
                    .ToList();

                return Json(new { success = true, claims = specialClaims });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Helper method để lấy tên hiển thị của claim type
        private string GetClaimDisplayName(string claimType)
        {
            var claimNames = new Dictionary<string, string>
            {
                { WebHSClaimTypes.SuperHost, "Super Host" },
                { WebHSClaimTypes.CanViewReports, "Xem báo cáo" },
                { WebHSClaimTypes.CanModerateReviews, "Kiểm duyệt đánh giá" },
                { WebHSClaimTypes.CanManagePromotions, "Quản lý khuyến mãi" },
                { WebHSClaimTypes.CanManageUsers, "Quản lý người dùng" },
                { WebHSClaimTypes.CanManageProperties, "Quản lý bất động sản" },
                { WebHSClaimTypes.CanApproveListings, "Duyệt tin đăng" },
                { WebHSClaimTypes.CanAccessApiKeys, "Truy cập API Keys" },
                { WebHSClaimTypes.CanManageRoles, "Quản lý vai trò" }
            };

            return claimNames.GetValueOrDefault(claimType, claimType);
        }

        // Helper method để lấy tên hiển thị tiếng Việt của claim type
        private string GetClaimTypeDisplayName(string claimType)
        {
            var claimTypeNames = new Dictionary<string, string>
            {
                // Claims hệ thống chuẩn
                { "email", "Email" },
                { "name", "Tên" },
                { "given_name", "Tên riêng" },
                { "family_name", "Họ" },
                { "birthdate", "Ngày sinh" },
                { "address", "Địa chỉ" },
                { "phone_number", "Số điện thoại" },
                { "language", "Ngôn ngữ" },
                { "role", "Vai trò" },
                
                // Claims xác thực
                { "phone_verified", "Xác thực SĐT" },
                { "email_verified", "Xác thực Email" },
                { "identity_verified", "Xác thực danh tính" },
                
                // Claims cấp độ
                { "account_level", "Cấp độ tài khoản" },
                { "membership_tier", "Hạng thành viên" },
                
                // Claims quyền hạn
                { WebHSClaimTypes.SuperHost, "Siêu chủ nhà" },
                { WebHSClaimTypes.CanViewReports, "Quyền xem báo cáo" },
                { WebHSClaimTypes.CanModerateReviews, "Quyền kiểm duyệt đánh giá" },
                { WebHSClaimTypes.CanManagePromotions, "Quyền quản lý khuyến mãi" },
                { WebHSClaimTypes.CanManageUsers, "Quyền quản lý người dùng" },
                { WebHSClaimTypes.CanManageProperties, "Quyền quản lý bất động sản" },
                { WebHSClaimTypes.CanApproveListings, "Quyền duyệt tin đăng" },
                { WebHSClaimTypes.CanAccessApiKeys, "Quyền truy cập API Keys" },
                { WebHSClaimTypes.CanManageRoles, "Quyền quản lý vai trò" }
            };

            return claimTypeNames.GetValueOrDefault(claimType, claimType);
        }

        // Helper method để lấy tên hiển thị tiếng Việt của claim value
        private string GetClaimValueDisplayName(string claimType, string claimValue)
        {
            if (string.IsNullOrEmpty(claimValue))
                return claimValue ?? "";

            // Xử lý các giá trị boolean
            if (claimValue.ToLower() == "true")
                return "Có";
            if (claimValue.ToLower() == "false")
                return "Không";

            // Xử lý các giá trị cụ thể theo loại claim
            switch (claimType)
            {
                case "account_level":
                    return claimValue.ToLower() switch
                    {
                        "basic" => "Cơ bản",
                        "premium" => "Cao cấp",
                        "vip" => "VIP",
                        _ => claimValue
                    };

                case "membership_tier":
                    return claimValue.ToLower() switch
                    {
                        "bronze" => "Đồng",
                        "silver" => "Bạc",
                        "gold" => "Vàng",
                        "platinum" => "Bạch kim",
                        _ => claimValue
                    };

                case "language":
                    return claimValue.ToLower() switch
                    {
                        "vi" => "Tiếng Việt",
                        "en" => "Tiếng Anh",
                        "fr" => "Tiếng Pháp",
                        "de" => "Tiếng Đức",
                        "ja" => "Tiếng Nhật",
                        "ko" => "Tiếng Hàn",
                        "zh" => "Tiếng Trung",
                        _ => claimValue
                    };

                default:
                    return claimValue;
            }
        }

        #endregion

        #region Role Claims

        // Hiển thị claims của một role
        public async Task<IActionResult> RoleClaims(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return NotFound();

            var roleClaims = await _claimService.GetRoleClaimsAsync(roleName);

            var model = new RoleClaimsViewModel
            {
                RoleId = role.Id,
                RoleName = roleName,
                Claims = roleClaims.Select(c => new ClaimViewModel
                {
                    ClaimType = c.Type,
                    ClaimValue = c.Value
                }).ToList()
            };

            return View(model);
        }

        // Hiển thị form thêm claim cho role
        [HttpGet]
        public IActionResult AddRoleClaim(string roleId, string roleName)
        {
            var model = new ClaimViewModel
            {
                RoleId = roleId,
                RoleName = roleName
            };
            return View(model);
        }

        // Xử lý thêm claim cho role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoleClaim(ClaimViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.RoleId))
            {
                ModelState.AddModelError("", "Role ID là bắt buộc.");
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
                return NotFound();

            await _claimService.AddRoleClaimAsync(role.Name ?? string.Empty, model.ClaimType, model.ClaimValue);
            
            return RedirectToAction(nameof(RoleClaims), new { roleName = role.Name });
        }

        // Xử lý xóa claim của role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoleClaim(string roleId, string claimType, string claimValue)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return NotFound();

            await _claimService.RemoveRoleClaimAsync(role.Name ?? string.Empty, claimType ?? string.Empty, claimValue ?? string.Empty);

            return RedirectToAction(nameof(RoleClaims), new { roleName = role.Name });
        }

        #endregion
    }
}
