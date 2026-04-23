using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebHS.Data;
using WebHS.Models;
using WebHS.Services;

namespace WebHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClaimsSeedController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IClaimManagementService _claimService;

        public ClaimsSeedController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IClaimManagementService claimService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _claimService = claimService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedRoleClaims()
        {
            try
            {
                // Lấy ra các vai trò
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                var hostRole = await _roleManager.FindByNameAsync("Host");
                var userRole = await _roleManager.FindByNameAsync("User");

                if (adminRole == null || hostRole == null || userRole == null)
                {
                    TempData["Error"] = "Không tìm thấy đủ các vai trò cơ bản (Admin, Host, User).";
                    return RedirectToAction("Index");
                }

                // Claims cho vai trò Admin
                var adminClaims = new Dictionary<string, string>
                {
                    { WebHSClaimTypes.CanManageUsers, "true" },
                    { WebHSClaimTypes.CanManageRoles, "true" },
                    { WebHSClaimTypes.CanViewReports, "true" },
                    { WebHSClaimTypes.CanManageProperties, "true" },
                    { WebHSClaimTypes.CanApproveListings, "true" },
                    { WebHSClaimTypes.CanModerateReviews, "true" },
                    { WebHSClaimTypes.CanManagePromotions, "true" },
                    { WebHSClaimTypes.CanAccessApiKeys, "true" }
                };

                // Claims cho vai trò Host
                var hostClaims = new Dictionary<string, string>
                {
                    { WebHSClaimTypes.CanManageProperties, "true" },
                    { WebHSClaimTypes.HostVerified, "true" }
                };

                // Claims cho vai trò User
                var userClaims = new Dictionary<string, string>
                {
                    // Không có quyền đặc biệt
                };

                // Thêm claims cho vai trò Admin
                foreach (var claim in adminClaims)
                {
                    // Kiểm tra claim đã tồn tại chưa
                    var existingClaims = await _roleManager.GetClaimsAsync(adminRole);
                    if (!existingClaims.Any(c => c.Type == claim.Key && c.Value == claim.Value))
                        await _roleManager.AddClaimAsync(adminRole, new Claim(claim.Key, claim.Value));
                }

                // Thêm claims cho vai trò Host
                foreach (var claim in hostClaims)
                {
                    // Kiểm tra claim đã tồn tại chưa
                    var existingClaims = await _roleManager.GetClaimsAsync(hostRole);
                    if (!existingClaims.Any(c => c.Type == claim.Key && c.Value == claim.Value))
                        await _roleManager.AddClaimAsync(hostRole, new Claim(claim.Key, claim.Value));
                }

                // Thêm claims cho vai trò User
                foreach (var claim in userClaims)
                {
                    // Kiểm tra claim đã tồn tại chưa
                    var existingClaims = await _roleManager.GetClaimsAsync(userRole);
                    if (!existingClaims.Any(c => c.Type == claim.Key && c.Value == claim.Value))
                        await _roleManager.AddClaimAsync(userRole, new Claim(claim.Key, claim.Value));
                }

                TempData["Message"] = "Đã thêm thành công các claims cho vai trò.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khởi tạo claims cho vai trò: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedSuperHostClaims()
        {
            try
            {
                // Lấy ra các host có role "Host"
                var hostUsers = new List<User>();
                var allUsers = await _context.Users.ToListAsync();
                
                foreach (var user in allUsers)
                {
                    if (await _userManager.IsInRoleAsync(user, "Host"))
                    {
                        hostUsers.Add(user);
                    }
                }

                if (!hostUsers.Any())
                {
                    TempData["Error"] = "Không tìm thấy host nào để gán SuperHost.";
                    return RedirectToAction("Index");
                }
                
                // Với mỗi host, cập nhật hoặc thêm claim SuperHost = true
                foreach (var host in hostUsers)
                {
                    // Thêm thông tin SuperHost
                    await _claimService.AddUserClaimAsync(host, WebHSClaimTypes.SuperHost, "true");
                    
                    // Thông tin bổ sung cho host
                    await _claimService.AddUserClaimAsync(host, WebHSClaimTypes.HostSince, DateTime.UtcNow.AddYears(-1).ToString("yyyy-MM-dd"));
                    await _claimService.AddUserClaimAsync(host, WebHSClaimTypes.HostRating, "4.8");
                    await _claimService.AddUserClaimAsync(host, WebHSClaimTypes.IdentityVerified, "true");
                    
                    // Random số lượng homestay quản lý (1-10)
                    Random random = new Random();
                    int propertyCount = random.Next(1, 11);
                    await _claimService.AddUserClaimAsync(host, WebHSClaimTypes.PropertyCount, propertyCount.ToString());
                }

                TempData["Message"] = "Đã thêm thành công các claims cho Super Host.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khởi tạo claims cho Super Host: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedAdminClaims()
        {
            try
            {
                // Lấy ra tất cả admin
                var adminRoleName = "Admin";
                var admins = await _userManager.GetUsersInRoleAsync(adminRoleName);

                if (!admins.Any())
                {
                    TempData["Error"] = "Không tìm thấy admin nào.";
                    return RedirectToAction("Index");
                }

                foreach (var admin in admins)
                {
                    // Claims bổ sung cho admin
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.CanManageUsers, "true");
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.CanManageRoles, "true");
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.CanViewReports, "true");
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.CanManageProperties, "true");
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.CanManagePromotions, "true");
                    await _claimService.AddUserClaimAsync(admin, WebHSClaimTypes.AccountLevel, "admin");
                }

                TempData["Message"] = "Đã thêm thành công các claims cho Admin.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khởi tạo claims cho Admin: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedUserProfileClaims()
        {
            try
            {
                // Lấy ra tất cả người dùng 
                var users = await _context.Users.ToListAsync();

                if (!users.Any())
                {
                    TempData["Error"] = "Không tìm thấy người dùng nào.";
                    return RedirectToAction("Index");
                }

                Random random = new Random();
                
                foreach (var user in users)
                {
                    // Claims thông tin cá nhân cho mọi người dùng
                    await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.DateOfBirth, 
                        new DateTime(random.Next(1970, 2000), random.Next(1, 13), random.Next(1, 29)).ToString("yyyy-MM-dd"));
                    
                    await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.PreferredLanguage, 
                        random.Next(0, 10) > 3 ? "vi-VN" : "en-US");
                    
                    await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.PhoneVerified, 
                        random.Next(0, 10) > 2 ? "true" : "false");
                    
                    await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.ProfileCompleted, 
                        random.Next(0, 10) > 3 ? "true" : "false");
                    
                    // Cấp độ tài khoản ngẫu nhiên
                    string[] levels = { "basic", "premium", "vip" };
                    await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.AccountLevel, 
                        levels[random.Next(0, levels.Length)]);
                    
                    // Thêm ngẫu nhiên các thuộc tính khác
                    if (random.Next(0, 10) > 5)
                    {
                        string[] loginMethods = { "password", "google", "facebook" };
                        await _claimService.AddUserClaimAsync(user, WebHSClaimTypes.LoginMethod,
                            loginMethods[random.Next(0, loginMethods.Length)]);
                    }
                }

                TempData["Message"] = "Đã thêm thành công các claims thông tin cá nhân cho người dùng.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi khởi tạo claims thông tin cá nhân: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
