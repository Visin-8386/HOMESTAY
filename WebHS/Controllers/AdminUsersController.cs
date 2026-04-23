using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Services;
using System.Security.Claims;

namespace WebHS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Hiển thị danh sách tất cả người dùng
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                // Lấy roles của user từ AspNetUserRoles
                var roles = await _userManager.GetRolesAsync(user);
                
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim(),
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive,
                    Roles = roles.ToList() // Thêm roles vào ViewModel
                });
            }

            return View(userViewModels);
        }

        // Hiển thị chi tiết một người dùng
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var model = new UserDetailsViewModel
            {
                Id = user.Id ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                ProfilePicture = user.ProfilePicture ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles.ToList(),
                Claims = claims.Select(c => new ClaimViewModel 
                { 
                    ClaimType = c.Type, 
                    ClaimValue = c.Value,
                    UserId = user.Id
                }).ToList()
            };

            return View(model);
        }

        // Hiển thị form chỉnh sửa một người dùng
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                ProfilePicture = user.ProfilePicture ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                IsActive = user.IsActive,
                AllRoles = allRoles.Select(r => new RoleViewModel
                {
                    Id = r.Id ?? string.Empty,
                    Name = r.Name ?? string.Empty,
                    IsSelected = userRoles.Contains(r.Name ?? string.Empty)
                }).ToList(),
                SelectedRoles = userRoles.ToList()
            };

            return View(model);
        }

        // Xử lý form chỉnh sửa một người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Id))
                return BadRequest("User ID is required");

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName ?? string.Empty;
            user.LastName = model.LastName ?? string.Empty;
            user.PhoneNumber = model.PhoneNumber ?? string.Empty;
            user.Address = model.Address ?? string.Empty;
            user.Bio = model.Bio ?? string.Empty;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Cập nhật vai trò
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in model.AllRoles)
            {
                var roleName = role.Name ?? string.Empty;
                if (role.IsSelected && !userRoles.Contains(roleName))
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
                else if (!role.IsSelected && userRoles.Contains(roleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleName);
                }
            }

            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        // Xóa người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Không thể xóa người dùng này.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        // Hiển thị danh sách tất cả vai trò
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleViewModel
                {
                    Id = r.Id ?? string.Empty,
                    Name = r.Name ?? string.Empty
                })
                .ToListAsync();

            return View(roles);
        }

        // Hiển thị chi tiết một vai trò
        public async Task<IActionResult> RoleDetails(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var users = new List<UserViewModel>();
            foreach (var user in await _userManager.Users.ToListAsync())
            {
                if (await _userManager.IsInRoleAsync(user, role.Name ?? string.Empty))
                {
                    users.Add(new UserViewModel
                    {
                        Id = user.Id ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        FullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}"
                    });
                }
            }

            var claims = await _roleManager.GetClaimsAsync(role);

            var model = new RoleDetailsViewModel
            {
                Id = role.Id ?? string.Empty,
                Name = role.Name ?? string.Empty,
                Users = users,
                Claims = claims.Select(c => new ClaimViewModel 
                { 
                    ClaimType = c.Type, 
                    ClaimValue = c.Value,
                    RoleId = role.Id ?? string.Empty,
                    RoleName = role.Name ?? string.Empty
                }).ToList()
            };

            return View(model);
        }

        // Hiển thị form tạo vai trò mới
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        // Xử lý form tạo vai trò mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Name))
            {
                ModelState.AddModelError(string.Empty, "Role name is required");
                return View(model);
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Roles));
        }

        // Hiển thị form chỉnh sửa vai trò
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var model = new EditRoleViewModel
            {
                Id = role.Id ?? string.Empty,
                Name = role.Name ?? string.Empty
            };

            return View(model);
        }

        // Xử lý form chỉnh sửa vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Id))
                return BadRequest("Role ID is required");

            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
                return NotFound();

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(RoleDetails), new { id = role.Id });
        }

        // Xóa vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Không thể xóa vai trò này.";
                return RedirectToAction(nameof(RoleDetails), new { id = id });
            }

            return RedirectToAction(nameof(Roles));
        }

        // Quản lý người dùng trong một vai trò
        [HttpGet]
        public async Task<IActionResult> ManageUsersInRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return NotFound();

            var model = new UsersInRoleViewModel
            {
                RoleId = roleId ?? string.Empty,
                RoleName = role.Name ?? string.Empty
            };

            var users = await _userManager.Users.ToListAsync();
            foreach (var user in users)
            {
                var isInRole = await _userManager.IsInRoleAsync(user, role.Name ?? string.Empty);
                model.Users.Add(new UserRoleViewModel
                {
                    UserId = user.Id ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    FullName = $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}",
                    Email = user.Email ?? string.Empty,
                    IsSelected = isInRole
                });
            }

            return View(model);
        }

        // Xử lý quản lý người dùng trong một vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageUsersInRole(UsersInRoleViewModel model)
        {
            if (string.IsNullOrEmpty(model.RoleId))
                return BadRequest("Role ID is required");

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
                return NotFound();

            for (int i = 0; i < model.Users.Count; i++)
            {
                var user = await _userManager.FindByIdAsync(model.Users[i].UserId ?? string.Empty);
                
                if (user != null)
                {
                    var isInRole = await _userManager.IsInRoleAsync(user, role.Name ?? string.Empty);

                    if (model.Users[i].IsSelected && !isInRole)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name ?? string.Empty);
                    }
                    else if (!model.Users[i].IsSelected && isInRole)
                    {
                        await _userManager.RemoveFromRoleAsync(user, role.Name ?? string.Empty);
                    }
                }
            }

            return RedirectToAction(nameof(RoleDetails), new { id = model.RoleId });
        }

        // Toggle host status cho user với đồng bộ hóa role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHostStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("User ID is required");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found");

            // Kiểm tra xem user hiện tại có role "Host" không
            var isCurrentlyHost = await _userManager.IsInRoleAsync(user, "Host");
            
            // Toggle host role
            var action = string.Empty;
            if (isCurrentlyHost)
            {
                // Thu hồi quyền host
                var result = await _userManager.RemoveFromRoleAsync(user, "Host");
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = "Không thể thu hồi quyền host.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                action = "thu hồi quyền host";
            }
            else
            {
                // Cấp quyền host
                var result = await _userManager.AddToRoleAsync(user, "Host");
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = "Không thể cấp quyền host.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                action = "cấp quyền host";
            }

            // Cập nhật thời gian
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Đã {action} cho người dùng {user.FirstName} {user.LastName}.";

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Cấp quyền host cho nhiều user cùng lúc với đồng bộ hóa role
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> BulkGrantHostStatus([FromBody] BulkHostStatusRequest request)
        {
            if (request?.UserIds == null || !request.UserIds.Any())
                return BadRequest("No users selected");

            var successCount = 0;
            var errorMessages = new List<string>();

            foreach (var userId in request.UserIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    errorMessages.Add($"User with ID {userId} not found");
                    continue;
                }

                try
                {
                    var isCurrentlyHost = await _userManager.IsInRoleAsync(user, "Host");
                    
                    if (request.GrantHost && !isCurrentlyHost)
                    {
                        // Cấp quyền host
                        var result = await _userManager.AddToRoleAsync(user, "Host");
                        if (result.Succeeded)
                        {
                            user.UpdatedAt = DateTime.UtcNow;
                            await _userManager.UpdateAsync(user);
                            successCount++;
                        }
                        else
                        {
                            errorMessages.Add($"Failed to grant host role to {user.FirstName} {user.LastName}");
                        }
                    }
                    else if (!request.GrantHost && isCurrentlyHost)
                    {
                        // Thu hồi quyền host
                        var result = await _userManager.RemoveFromRoleAsync(user, "Host");
                        if (result.Succeeded)
                        {
                            user.UpdatedAt = DateTime.UtcNow;
                            await _userManager.UpdateAsync(user);
                            successCount++;
                        }
                        else
                        {
                            errorMessages.Add($"Failed to revoke host role from {user.FirstName} {user.LastName}");
                        }
                    }
                    else
                    {
                        // User đã ở trạng thái mong muốn
                        successCount++;
                    }
                }
                catch (Exception)
                {
                    errorMessages.Add($"Error updating {user.FirstName} {user.LastName}");
                }
            }

            return Json(new
            {
                success = true,
                message = $"Đã cập nhật {successCount}/{request.UserIds.Count} người dùng thành công.",
                errors = errorMessages
            });
        }

        // API endpoint để kiểm tra trạng thái host
        [HttpGet]
        public async Task<IActionResult> GetHostStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("User ID is required");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var isHost = await _userManager.IsInRoleAsync(user, "Host");

            return Json(new
            {
                userId = user.Id,
                fullName = $"{user.FirstName} {user.LastName}",
                email = user.Email,
                isHost = isHost,
                isActive = user.IsActive,
                lastUpdated = user.UpdatedAt
            });
        }
        }
    }

    // Request model cho bulk host status update
    public class BulkHostStatusRequest
    {
        public List<string> UserIds { get; set; } = new();
        public bool GrantHost { get; set; }
    }
