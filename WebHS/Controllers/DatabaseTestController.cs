using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Controllers
{
    [AllowAnonymous]
    public class DatabaseTestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseTestController(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> CheckUserTokens()
        {
            try
            {
                // Get all user tokens
                var tokens = await _context.UserTokens
                    .Select(t => new
                    {
                        t.UserId,
                        t.LoginProvider,
                        t.Name,
                        TokenExists = !string.IsNullOrEmpty(t.Value),
                        TokenLength = t.Value != null ? t.Value.Length : 0
                    })
                    .ToListAsync();

                // Get users with their authentication methods
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.EmailConfirmed,
                        HasPassword = !string.IsNullOrEmpty(u.PasswordHash),
                        u.CreatedAt
                    })
                    .ToListAsync();

                // Get external logins
                var externalLogins = await _context.UserLogins
                    .Select(l => new
                    {
                        l.UserId,
                        l.LoginProvider,
                        l.ProviderKey
                    })
                    .ToListAsync();

                var result = new
                {
                    TotalUsers = users.Count,
                    UsersWithPassword = users.Count(u => u.HasPassword),
                    UsersWithoutPassword = users.Count(u => !u.HasPassword),
                    EmailConfirmedUsers = users.Count(u => u.EmailConfirmed),
                    EmailUnconfirmedUsers = users.Count(u => !u.EmailConfirmed),
                    TotalTokens = tokens.Count,
                    ExternalLogins = externalLogins.Count,
                    TokensByProvider = tokens.GroupBy(t => t.LoginProvider)
                        .Select(g => new { Provider = g.Key, Count = g.Count() })
                        .ToList(),
                    TokensByName = tokens.GroupBy(t => t.Name)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .ToList(),
                    ExternalLoginsByProvider = externalLogins.GroupBy(l => l.LoginProvider)
                        .Select(g => new { Provider = g.Key, Count = g.Count() })
                        .ToList(),
                    SampleTokens = tokens.Take(10).ToList(),
                    SampleUsers = users.Take(5).ToList(),
                    SampleExternalLogins = externalLogins.Take(5).ToList()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugTokenCreation()
        {
            try
            {
                // Find a test user or create one
                var testEmail = "test@example.com";
                var user = await _userManager.FindByEmailAsync(testEmail);
                
                if (user == null)
                {
                    user = new User
                    {
                        UserName = testEmail,
                        Email = testEmail,
                        FirstName = "Test",
                        LastName = "User",
                        EmailConfirmed = false
                    };
                    
                    var result = await _userManager.CreateAsync(user, "Test123!");
                    if (!result.Succeeded)
                    {
                        return Json(new { error = "Failed to create test user", errors = result.Errors });
                    }
                }

                // Check tokens before generation
                var tokensBefore = await _context.UserTokens
                    .Where(t => t.UserId == user.Id)
                    .Select(t => new { t.LoginProvider, t.Name, HasValue = !string.IsNullOrEmpty(t.Value) })
                    .ToListAsync();

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Generate password reset token  
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Check tokens after generation
                var tokensAfter = await _context.UserTokens
                    .Where(t => t.UserId == user.Id)
                    .Select(t => new { t.LoginProvider, t.Name, HasValue = !string.IsNullOrEmpty(t.Value) })
                    .ToListAsync();

                // Try to manually set a token
                await _userManager.SetAuthenticationTokenAsync(user, "TestProvider", "TestToken", "TestValue");
                
                // Check tokens after manual set
                var tokensAfterManual = await _context.UserTokens
                    .Where(t => t.UserId == user.Id)
                    .Select(t => new { t.LoginProvider, t.Name, HasValue = !string.IsNullOrEmpty(t.Value) })
                    .ToListAsync();

                return Json(new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    TokensBefore = tokensBefore,
                    EmailTokenGenerated = !string.IsNullOrEmpty(emailToken),
                    EmailTokenLength = emailToken?.Length ?? 0,
                    ResetTokenGenerated = !string.IsNullOrEmpty(resetToken),
                    ResetTokenLength = resetToken?.Length ?? 0,
                    TokensAfter = tokensAfter,
                    TokensAfterManual = tokensAfterManual,
                    IdentityConfiguration = new
                    {
                        RequireConfirmedEmail = _userManager.Options.SignIn.RequireConfirmedEmail,
                        TokenProviders = _userManager.Options.Tokens.ProviderMap.Keys.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckRolesAndClaims()
        {
            try
            {
                var result = new
                {
                    // 1. Kiểm tra tất cả roles
                    Roles = await _context.Roles
                        .Select(r => new { r.Id, r.Name, r.NormalizedName })
                        .OrderBy(r => r.Name)
                        .ToListAsync(),

                    // 2. Kiểm tra users và roles
                    UsersWithRoles = await (from u in _context.Users
                                          join ur in _context.UserRoles on u.Id equals ur.UserId into userRoles
                                          from ur in userRoles.DefaultIfEmpty()
                                          join r in _context.Roles on ur.RoleId equals r.Id into roles
                                          from r in roles.DefaultIfEmpty()
                                          select new
                                          {
                                              UserId = u.Id,
                                              UserName = u.UserName,
                                              Email = u.Email,
                                              RoleName = r.Name ?? "No Role"
                                          })
                                          .OrderBy(x => x.UserName)
                                          .ToListAsync(),

                    // 3. User claims
                    UserClaims = await (from u in _context.Users
                                       join uc in _context.UserClaims on u.Id equals uc.UserId
                                       select new
                                       {
                                           UserName = u.UserName,
                                           Email = u.Email,
                                           ClaimType = uc.ClaimType,
                                           ClaimValue = uc.ClaimValue
                                       })
                                       .OrderBy(x => x.UserName)
                                       .ThenBy(x => x.ClaimType)
                                       .ToListAsync(),

                    // 4. Role claims  
                    RoleClaims = await (from r in _context.Roles
                                       join rc in _context.RoleClaims on r.Id equals rc.RoleId
                                       select new
                                       {
                                           RoleName = r.Name,
                                           ClaimType = rc.ClaimType,
                                           ClaimValue = rc.ClaimValue
                                       })
                                       .OrderBy(x => x.RoleName)
                                       .ThenBy(x => x.ClaimType)
                                       .ToListAsync(),

                    // 5. Thống kê
                    Statistics = new
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        TotalRoles = await _context.Roles.CountAsync(),
                        TotalUserRoles = await _context.UserRoles.CountAsync(),
                        TotalUserClaims = await _context.UserClaims.CountAsync(),
                        TotalRoleClaims = await _context.RoleClaims.CountAsync(),
                        UsersWithoutRoles = await _context.Users
                            .Where(u => !_context.UserRoles.Any(ur => ur.UserId == u.Id))
                            .CountAsync()
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncHostStatusWithRoles()
        {
            try
            {
                var result = new
                {
                    totalUsers = 0,
                    syncedUsers = 0,
                    errors = new List<string>(),
                    details = new List<object>()
                };

                // Lấy tất cả users
                var users = await _context.Users.ToListAsync();
                result = new
                {
                    totalUsers = users.Count,
                    syncedUsers = 0,
                    errors = new List<string>(),
                    details = new List<object>()
                };

                var syncedCount = 0;
                var errors = new List<string>();
                var details = new List<object>();

                // Đảm bảo role "Host" tồn tại
                var hostRole = await _roleManager.FindByNameAsync("Host");
                if (hostRole == null)
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole("Host"));
                    if (createRoleResult.Succeeded)
                    {
                        details.Add(new { action = "Created Role 'Host'", success = true });
                    }
                    else
                    {
                        errors.Add("Failed to create Host role");
                    }
                }

                // Sync từng user - không còn dùng IsHost property nữa
                foreach (var user in users)
                {
                    try
                    {
                        var hasHostRole = await _userManager.IsInRoleAsync(user, "Host");
                        
                        if (hasHostRole)
                        {
                            syncedCount++;
                            details.Add(new 
                            { 
                                userId = user.Id, 
                                userName = user.UserName, 
                                action = "Has Host role", 
                                success = true 
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error checking user {user.UserName}: {ex.Message}");
                    }
                }

                return Json(new
                {
                    success = true,
                    totalUsers = users.Count,
                    syncedUsers = syncedCount,
                    errors = errors,
                    details = details,
                    message = $"Synced {syncedCount}/{users.Count} users successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
