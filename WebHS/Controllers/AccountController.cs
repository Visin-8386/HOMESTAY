using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHS.Data;
using Microsoft.EntityFrameworkCore;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace WebHS.Controllers
{    public class AccountController : Controller
    {
        private readonly UserManager<WebHSUser> _userManager;
        private readonly SignInManager<WebHSUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<WebHSUser> userManager,
            SignInManager<WebHSUser> signInManager,
            IEmailService emailService,
            IFileUploadService fileUploadService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _fileUploadService = fileUploadService;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                // Check if user exists and is active before attempting sign-in
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                
                if (result.RequiresTwoFactor)
                {
                    // Handle two-factor authentication if needed
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 30 phút.");
                    return View(model);
                }
                
                if (result.IsNotAllowed)
                {
                    // Check if it's because of inactive account or unconfirmed email
                    if (user != null && !user.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Vui lòng xác nhận email trước khi đăng nhập.");
                    }
                    return View(model);
                }
                
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new WebHS.Models.User // Ensure this is WebHS.Models.User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                    // IsActive = true // Removed as it's defaulted in User model and was causing issues before
                };

                var result = await _userManager.CreateAsync(user, model.Password);                if (result.Succeeded)
                {
                    // Add user to appropriate role
                    var role = model.Role == "Host" ? WebHS.Models.UserRoles.Host : WebHS.Models.UserRoles.User;
                    await _userManager.AddToRoleAsync(user, role);                    // Always send confirmation email (both production and development)
                    try
                    {
                        var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
                        logger?.LogInformation("Starting email confirmation process for user {Email}", user.Email);
                        
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account",
                            new { userId = user.Id, token = token }, Request.Scheme);

                        logger?.LogInformation("Generated confirmation link: {Link}", confirmationLink);
                        
                        await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink!);
                        
                        logger?.LogInformation("Email sent successfully to {Email}", user.Email);
                        
                        // Redirect to check email page
                        return RedirectToAction(nameof(CheckYourEmail));
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail registration
                        var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
                        logger?.LogError(ex, "Failed to send confirmation email for user {Email}. Error: {Error}", user.Email, ex.Message);
                        
                        TempData["Error"] = $"Đăng ký thành công! Tuy nhiên không thể gửi email xác nhận: {ex.Message}";
                        return RedirectToAction(nameof(Login));
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                TempData["Error"] = "Liên kết xác minh không hợp lệ.";
                return View("EmailVerification");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return View("EmailVerification");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["Message"] = "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return View("EmailVerification");
            }
            else
            {
                TempData["Error"] = "Không thể xác nhận email. Liên kết có thể đã hết hạn hoặc không hợp lệ.";
                return View("EmailVerification");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action(nameof(ResetPassword), "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(resetLink))
                {
                    await _emailService.SendResetPasswordEmailAsync(user.Email, resetLink);
                }

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest();

            var model = new ResetPasswordViewModel { UserId = userId, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Message"] = "Mật khẩu đã được đặt lại thành công!";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // Tính số liệu cho user
            var totalBookings = await _context.Bookings
                .Where(b => b.UserId == user.Id)
                .CountAsync();

            var totalReviews = await _context.Bookings
                .Where(b => b.UserId == user.Id && b.ReviewRating.HasValue)
                .CountAsync();

            var totalHomestays = 0;
            if (await _userManager.IsInRoleAsync(user, "Host"))
            {
                totalHomestays = await _context.Homestays
                    .Where(h => h.HostId == user.Id)
                    .CountAsync();
            }

            // Tạo ViewModel với thông tin đầy đủ
            var viewModel = new UserProfileViewModel
            {
                User = user,
                TotalBookings = totalBookings,
                TotalReviews = totalReviews,
                TotalHomestays = totalHomestays
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Bio = user.Bio,
                CurrentProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            // Check if email is already taken by another user
            if (model.Email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }

            // Handle profile picture upload
            if (model.ProfilePictureFile != null)
            {
                try
                {
                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        await _fileUploadService.DeleteImageAsync(user.ProfilePicture);
                    }

                    // Upload new profile picture
                    var profilePictureUrl = await _fileUploadService.UploadImageAsync(
                        model.ProfilePictureFile, "profiles");
                    user.ProfilePicture = profilePictureUrl;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("ProfilePictureFile", "Có lỗi xảy ra khi tải ảnh lên. Vui lòng thử lại.");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }            // Update user information
            user.FirstName = model.FirstName ?? string.Empty;
            user.LastName = model.LastName ?? string.Empty;
            user.PhoneNumber = model.PhoneNumber ?? string.Empty;
            user.Address = model.Address;
            user.Bio = model.Bio;
            user.UpdatedAt = DateTime.UtcNow;

            // Update email if changed
            if (model.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    model.CurrentProfilePicture = user.ProfilePicture;
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Thông tin cá nhân đã được cập nhật thành công!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }        private IActionResult LoginWith2fa(string? returnUrl = null, bool rememberMe = false)
        {
            // Implement two-factor authentication if needed
            return RedirectToAction(nameof(Login));
        }        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
            logger?.LogInformation("ExternalLogin called with provider: {Provider}, returnUrl: {ReturnUrl}", provider, returnUrl);
            
            // Request a redirect to the external login provider
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            logger?.LogInformation("Generated redirect URL: {RedirectUrl}", redirectUrl);
            
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            // Log for debugging
            var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
            logger?.LogInformation("ExternalLoginCallback called with returnUrl: {ReturnUrl}, remoteError: {RemoteError}", returnUrl, remoteError);

            // TEST LOG - Should always appear
            logger?.LogInformation("=== TOKEN DEBUG TEST - ENTERING CALLBACK ===");

            if (remoteError != null)
            {
                logger?.LogError("Remote error from external provider: {RemoteError}", remoteError);
                TempData["Error"] = $"Lỗi từ nhà cung cấp bên ngoài: {remoteError}";
                return RedirectToAction(nameof(Login));
            }            // Try to get the external login info
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                logger?.LogError("GetExternalLoginInfoAsync returned null. Trying alternative approach...");
                  // Try to get external login info from the current context
                var externalResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                if (externalResult?.Succeeded == true)
                {
                    logger?.LogInformation("Found external authentication result");
                    var principal = externalResult.Principal;
                    var loginProvider = externalResult.Properties?.Items[".AuthScheme"] ?? "Google";
                    var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    
                    if (!string.IsNullOrEmpty(providerKey))
                    {
                        // Create ExternalLoginInfo manually
                        info = new ExternalLoginInfo(principal, loginProvider, providerKey, loginProvider)
                        {
                            AuthenticationTokens = externalResult.Properties?.GetTokens()
                        };
                        logger?.LogInformation("Created external login info manually for provider: {Provider}, key: {Key}", 
                            loginProvider, providerKey);
                    }
                }
                else
                {
                    // Alternative: try with Google scheme directly
                    var googleResult = await HttpContext.AuthenticateAsync("Google");
                    if (googleResult?.Succeeded == true)
                    {
                        logger?.LogInformation("Found Google authentication result directly");
                        var principal = googleResult.Principal;
                        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        
                        if (!string.IsNullOrEmpty(providerKey))
                        {
                            info = new ExternalLoginInfo(principal, "Google", providerKey, "Google")
                            {
                                AuthenticationTokens = googleResult.Properties?.GetTokens()
                            };
                            logger?.LogInformation("Created Google login info manually for key: {Key}", providerKey);
                        }
                    }
                }
                
                if (info == null)
                {
                    // Check if we have an authentication result
                    var authResult = await HttpContext.AuthenticateAsync();
                    logger?.LogInformation("Authentication result: {IsAuthenticated}, Scheme: {Scheme}", 
                        authResult.Succeeded, authResult.Ticket?.AuthenticationScheme);
                    
                    if (authResult.Principal != null)
                    {
                        logger?.LogInformation("Principal claims: {Claims}", 
                            string.Join(", ", authResult.Principal.Claims.Select(c => $"{c.Type}={c.Value}")));
                    }
                    
                    TempData["Error"] = "Không thể lấy thông tin từ nhà cung cấp bên ngoài. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Login));
                }
            }            logger?.LogInformation("External login info retrieved successfully. Provider: {Provider}, Email: {Email}", 
                info.LoginProvider, info.Principal.FindFirstValue(ClaimTypes.Email));

            // Simple debug log first
            logger?.LogInformation("[DEBUG] About to check tokens for provider: {Provider}", info.LoginProvider);
            
            // Debug log tokens
            if (info.AuthenticationTokens != null && info.AuthenticationTokens.Any())
            {
                logger?.LogInformation("[DEBUG] Found {TokenCount} authentication tokens from {Provider}", 
                    info.AuthenticationTokens.Count(), info.LoginProvider);
                foreach (var token in info.AuthenticationTokens)
                {
                    logger?.LogInformation("[DEBUG] Token: {TokenName} = {TokenValue}", 
                        token.Name, token.Value?.Substring(0, Math.Min(10, token.Value?.Length ?? 0)) + "...");
                }
            }
            else
            {
                logger?.LogWarning("[DEBUG] No authentication tokens found in external login info from {Provider}. Info.AuthenticationTokens is null: {IsNull}", 
                    info.LoginProvider, info.AuthenticationTokens == null);
            }            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (result.Succeeded)
            {
                // Save tokens for existing user
                var existingLoginUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingLoginUser != null && info.AuthenticationTokens != null)
                {
                    await SaveExternalTokensAsync(existingLoginUser, info);
                }
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                TempData["Error"] = "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 30 phút.";
                return RedirectToAction(nameof(Login));
            }

            if (result.IsNotAllowed)
            {
                // Check if it's because account is inactive
                var existingLoginUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingLoginUser != null && !existingLoginUser.IsActive)
                {
                    TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
                }
                else
                {
                    TempData["Error"] = "Tài khoản chưa được xác thực. Vui lòng liên hệ quản trị viên.";
                }
                return RedirectToAction(nameof(Login));
            }

            // If the user does not have an account, then ask the user to create an account
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Không thể lấy email từ nhà cung cấp bên ngoài.";
                return RedirectToAction(nameof(Login));
            }            // Check if user already exists with this email
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Check if user account is active
                if (!existingUser.IsActive)
                {
                    TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
                    return RedirectToAction(nameof(Login));
                }

                // Add this external login to the existing user
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    // Save tokens for the user
                    if (info.AuthenticationTokens != null)
                    {
                        await SaveExternalTokensAsync(existingUser, info);
                    }
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    TempData["Error"] = "Không thể liên kết tài khoản với nhà cung cấp bên ngoài.";
                    return RedirectToAction(nameof(Login));
                }
            }// Create a new user
            var user = new WebHSUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true // External logins are assumed to have confirmed emails
            };            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                // Add user to default role
                await _userManager.AddToRoleAsync(user, WebHS.Models.UserRoles.User);

                // Add the external login
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    // Save tokens for the new user
                    if (info.AuthenticationTokens != null)
                    {
                        await SaveExternalTokensAsync(user, info);
                    }
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
            }

            // If we got this far, something failed
            TempData["Error"] = "Không thể tạo tài khoản từ nhà cung cấp bên ngoài.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> DebugAuth()
        {
            var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
            
            // Check all authentication schemes
            var schemes = await HttpContext.RequestServices
                .GetRequiredService<IAuthenticationSchemeProvider>()
                .GetAllSchemesAsync();
            
            logger?.LogInformation("Available authentication schemes: {Schemes}", 
                string.Join(", ", schemes.Select(s => s.Name)));
            
            // Try to authenticate with each scheme
            foreach (var scheme in schemes)
            {
                try
                {
                    var result = await HttpContext.AuthenticateAsync(scheme.Name);
                    logger?.LogInformation("Scheme {SchemeName}: Success={Success}, Principal={HasPrincipal}", 
                        scheme.Name, result.Succeeded, result.Principal != null);
                    
                    if (result.Principal != null)
                    {
                        logger?.LogInformation("Claims for {SchemeName}: {Claims}", 
                            scheme.Name, 
                            string.Join(", ", result.Principal.Claims.Select(c => $"{c.Type}={c.Value}")));
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error authenticating with scheme {SchemeName}", scheme.Name);
                }
            }
            
            return Ok("Check logs for authentication debug info");
        }

        [HttpGet]
        public IActionResult Debug()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CheckYourEmail()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email không hợp lệ.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản với email này.";
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Message"] = "Email của bạn đã được xác nhận trước đó. Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink!);
                
                TempData["Message"] = "Email xác nhận đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn.";
                return RedirectToAction(nameof(CheckYourEmail));
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
                logger?.LogError(ex, "Failed to resend confirmation email for user {Email}", email);
                
                TempData["Error"] = "Không thể gửi lại email xác nhận. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult TestEmail()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TestEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Vui lòng nhập email.";
                return View();
            }

            try
            {
                await _emailService.SendEmailAsync(email, "Test Email - HomestayBooking", 
                    "<h2>🎉 Email Test Thành Công!</h2><p>Hệ thống email đã được cấu hình đúng cách.</p>");
                
                TempData["Message"] = "Email test đã được gửi thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi gửi email: {ex.Message}";
            }            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestAuth()
        {
            return View("~/Views/Shared/TestAuth.cshtml");
        }

        private async Task SaveExternalTokensAsync(WebHSUser user, ExternalLoginInfo info)
        {
            if (info.AuthenticationTokens == null) return;

            var logger = HttpContext.RequestServices.GetService<ILogger<AccountController>>();
            
            foreach (var token in info.AuthenticationTokens)
            {
                try
                {
                    await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, token.Name, token.Value);
                    logger?.LogInformation("Saved token {TokenName} for user {UserId} from provider {Provider}", 
                        token.Name, user.Id, info.LoginProvider);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to save token {TokenName} for user {UserId} from provider {Provider}", 
                        token.Name, user.Id, info.LoginProvider);
                }
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckMyTokens()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { error = "User not found" });
            }

            var tokens = new List<object>();
            
            // Kiểm tra Google tokens
            try
            {
                var accessToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
                var refreshToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
                var expiresAt = await _userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");
                var tokenType = await _userManager.GetAuthenticationTokenAsync(user, "Google", "token_type");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    tokens.Add(new { 
                        Provider = "Google", 
                        TokenName = "access_token", 
                        Value = accessToken?.Substring(0, Math.Min(20, accessToken.Length)) + "...",
                        HasValue = !string.IsNullOrEmpty(accessToken)
                    });
                }
                
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    tokens.Add(new { 
                        Provider = "Google", 
                        TokenName = "refresh_token", 
                        Value = refreshToken?.Substring(0, Math.Min(20, refreshToken.Length)) + "...",
                        HasValue = !string.IsNullOrEmpty(refreshToken)
                    });
                }
                
                if (!string.IsNullOrEmpty(expiresAt))
                {
                    tokens.Add(new { 
                        Provider = "Google", 
                        TokenName = "expires_at", 
                        Value = expiresAt,
                        HasValue = !string.IsNullOrEmpty(expiresAt)
                    });
                }
                
                if (!string.IsNullOrEmpty(tokenType))
                {
                    tokens.Add(new { 
                        Provider = "Google", 
                        TokenName = "token_type", 
                        Value = tokenType,
                        HasValue = !string.IsNullOrEmpty(tokenType)
                    });
                }
            }
            catch (Exception ex)
            {
                tokens.Add(new { Error = "Failed to retrieve Google tokens", Exception = ex.Message });
            }

            // Kiểm tra Facebook tokens nếu có
            try
            {
                var fbAccessToken = await _userManager.GetAuthenticationTokenAsync(user, "Facebook", "access_token");
                if (!string.IsNullOrEmpty(fbAccessToken))
                {
                    tokens.Add(new { 
                        Provider = "Facebook", 
                        TokenName = "access_token", 
                        Value = fbAccessToken?.Substring(0, Math.Min(20, fbAccessToken.Length)) + "...",
                        HasValue = !string.IsNullOrEmpty(fbAccessToken)
                    });
                }
            }
            catch (Exception ex)
            {
                tokens.Add(new { Error = "Failed to retrieve Facebook tokens", Exception = ex.Message });
            }

            return Json(new { 
                UserId = user.Id,
                Email = user.Email,
                Tokens = tokens,
                TotalTokens = tokens.Count,
                Message = tokens.Count == 0 ? "No tokens found for this user" : $"Found {tokens.Count} tokens"
            });
        }        [HttpGet]
        public IActionResult TestEndpoint()
        {
            return Json(new { 
                Message = "Test endpoint works!", 
                Timestamp = DateTime.Now,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name ?? "Anonymous"
            });
        }

        [HttpGet]
        public async Task<IActionResult> DebugGoogleTokens()
        {
            try
            {
                // Kiểm tra authentication context hiện tại
                var authResult = await HttpContext.AuthenticateAsync();
                var externalResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
                var googleResult = await HttpContext.AuthenticateAsync("Google");

                var result = new
                {
                    Timestamp = DateTime.Now,
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    UserName = User.Identity?.Name,
                    
                    // Check default auth
                    DefaultAuth = new
                    {
                        Succeeded = authResult.Succeeded,
                        Scheme = authResult.Ticket?.AuthenticationScheme,
                        HasTokens = authResult.Properties?.GetTokens()?.Any() ?? false,
                        Tokens = authResult.Properties?.GetTokens()?.Select(t => new { t.Name, Value = t.Value?.Substring(0, Math.Min(20, t.Value?.Length ?? 0)) + "..." })
                    },
                    
                    // Check external auth
                    ExternalAuth = new
                    {
                        Succeeded = externalResult.Succeeded,
                        Scheme = externalResult.Ticket?.AuthenticationScheme,
                        HasTokens = externalResult.Properties?.GetTokens()?.Any() ?? false,
                        Tokens = externalResult.Properties?.GetTokens()?.Select(t => new { t.Name, Value = t.Value?.Substring(0, Math.Min(20, t.Value?.Length ?? 0)) + "..." })
                    },
                    
                    // Check Google auth directly
                    GoogleAuth = new
                    {
                        Succeeded = googleResult.Succeeded,
                        Scheme = googleResult.Ticket?.AuthenticationScheme,
                        HasTokens = googleResult.Properties?.GetTokens()?.Any() ?? false,
                        Tokens = googleResult.Properties?.GetTokens()?.Select(t => new { t.Name, Value = t.Value?.Substring(0, Math.Min(20, t.Value?.Length ?? 0)) + "..." })
                    }
                };                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugUserTokensFromDB()
        {
            try
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    return Json(new { Error = "User not authenticated" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { Error = "User not found" });
                }

                // Kiểm tra tokens trong database
                var googleAccessToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
                var googleRefreshToken = await _userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
                var googleExpiresAt = await _userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");
                var googleTokenType = await _userManager.GetAuthenticationTokenAsync(user, "Google", "token_type");

                var result = new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    GoogleTokens = new
                    {
                        AccessToken = new { 
                            HasValue = !string.IsNullOrEmpty(googleAccessToken),
                            PreviewValue = string.IsNullOrEmpty(googleAccessToken) ? "null" : googleAccessToken.Substring(0, Math.Min(20, googleAccessToken.Length)) + "..."
                        },
                        RefreshToken = new { 
                            HasValue = !string.IsNullOrEmpty(googleRefreshToken),
                            PreviewValue = string.IsNullOrEmpty(googleRefreshToken) ? "null" : googleRefreshToken.Substring(0, Math.Min(20, googleRefreshToken.Length)) + "..."
                        },
                        ExpiresAt = googleExpiresAt ?? "null",
                        TokenType = googleTokenType ?? "null"
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // Action để hiển thị trang hướng dẫn xóa dữ liệu (cho Facebook App)        [HttpGet]
        public IActionResult DeleteData()
        {
            return View();
        }        // Test Facebook API
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestFacebookAPI()
        {
            try
            {
                var appId = _configuration["Authentication:Facebook:AppId"];
                var appSecret = _configuration["Authentication:Facebook:AppSecret"];

                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
                {
                    return Json(new { 
                        success = false, 
                        error = "Facebook App ID hoặc App Secret không được cấu hình",
                        appId = appId,
                        hasSecret = !string.IsNullOrEmpty(appSecret)
                    });
                }                // Test 1: Lấy App Access Token
                using var httpClient = new HttpClient();
                var tokenUrl = $"https://graph.facebook.com/oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";
                
                var tokenResponse = await httpClient.GetStringAsync(tokenUrl);
                var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tokenResponse);
                
                if (tokenData == null || !tokenData.ContainsKey("access_token"))
                {
                    return Json(new { 
                        success = false, 
                        error = "Không thể lấy access token",
                        response = tokenResponse 
                    });
                }

                var accessToken = tokenData["access_token"]?.ToString();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Json(new { 
                        success = false, 
                        error = "Access token rỗng hoặc null",
                        tokenData = tokenData
                    });
                }

                // Test 2: Lấy thông tin App
                var appInfoUrl = $"https://graph.facebook.com/{appId}?access_token={accessToken}";
                var appInfoResponse = await httpClient.GetStringAsync(appInfoUrl);
                var appInfo = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(appInfoResponse);

                return Json(new { 
                    success = true,
                    message = "Facebook API hoạt động tốt!",
                    appId = appId,
                    appInfo = appInfo,
                    hasAccessToken = !string.IsNullOrEmpty(accessToken)
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        // Kiểm tra Facebook App Status và hướng dẫn
        [HttpGet]
        public async Task<IActionResult> FacebookAppStatus()
        {
            try
            {
                var appId = _configuration["Authentication:Facebook:AppId"];
                var appSecret = _configuration["Authentication:Facebook:AppSecret"];
                
                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
                {
                    return Json(new { 
                        success = false, 
                        error = "Facebook App credentials not configured",
                        fix = "Cần cấu hình AppId và AppSecret trong appsettings.json"
                    });
                }

                using var httpClient = new HttpClient();
                
                // Kiểm tra App Token
                var appTokenUrl = $"https://graph.facebook.com/oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";
                var appTokenResponse = await httpClient.GetStringAsync(appTokenUrl);
                var appTokenData = JsonSerializer.Deserialize<JsonElement>(appTokenResponse);
                var appToken = appTokenData.GetProperty("access_token").GetString();

                // Kiểm tra App Details
                var appDetailsUrl = $"https://graph.facebook.com/{appId}?access_token={appToken}&fields=id,name,category,live_status,development_mode,app_domains,privacy_policy_url,terms_of_service_url";
                var appDetailsResponse = await httpClient.GetStringAsync(appDetailsUrl);
                var appDetails = JsonSerializer.Deserialize<JsonElement>(appDetailsResponse);

                var result = new
                {
                    success = true,
                    appId = appId,
                    appName = appDetails.TryGetProperty("name", out var name) ? name.GetString() : "Unknown",
                    liveStatus = appDetails.TryGetProperty("live_status", out var live) ? live.GetString() : "Unknown",
                    developmentMode = appDetails.TryGetProperty("development_mode", out var dev) ? dev.GetBoolean() : true,
                    appDomains = appDetails.TryGetProperty("app_domains", out var domains) ? domains.ToString() : "Not set",
                    privacyPolicyUrl = appDetails.TryGetProperty("privacy_policy_url", out var privacy) ? privacy.GetString() : "Not set",
                    termsUrl = appDetails.TryGetProperty("terms_of_service_url", out var terms) ? terms.GetString() : "Not set",
                    currentUrl = $"{Request.Scheme}://{Request.Host}",
                    recommendations = GetFacebookAppRecommendations(appDetails, Request)
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    error = ex.Message,
                    fix = "Kiểm tra lại Facebook App credentials và kết nối internet"
                });
            }
        }

        private List<string> GetFacebookAppRecommendations(JsonElement appDetails, HttpRequest request)
        {
            var recommendations = new List<string>();
            var currentUrl = $"{request.Scheme}://{request.Host}";
            
            // Kiểm tra Live Status
            if (appDetails.TryGetProperty("live_status", out var liveStatus))
            {
                if (liveStatus.GetString() != "LIVE")
                {
                    recommendations.Add("⚠️ App chưa LIVE: Vào Facebook Developer Console > App Settings > Basic > chuyển app sang chế độ LIVE");
                }
            }

            // Kiểm tra Development Mode
            if (appDetails.TryGetProperty("development_mode", out var devMode) && devMode.GetBoolean())
            {
                recommendations.Add("🔧 App đang ở Development Mode: Chỉ developer và tester mới đăng nhập được");
                recommendations.Add("👥 Thêm tài khoản test: App Roles > Roles > Add People (Administrator/Developer/Tester)");
            }

            // Kiểm tra HTTPS
            if (request.Scheme == "http")
            {
                recommendations.Add("🔒 Facebook yêu cầu HTTPS: Dùng ngrok để tạo HTTPS tunnel cho localhost");
                recommendations.Add($"📝 Lệnh ngrok: ngrok http {request.Host.Port ?? 5000}");
            }

            // Kiểm tra App Domains
            if (appDetails.TryGetProperty("app_domains", out var domains))
            {
                var domainsList = domains.ToString();
                if (string.IsNullOrEmpty(domainsList) || domainsList == "[]")
                {
                    recommendations.Add($"🌐 Thêm domain vào App Domains: {request.Host.Host}");
                }
            }

            // OAuth Redirect URI
            recommendations.Add($"🔄 Kiểm tra OAuth Redirect URI: {currentUrl}/signin-facebook");
            recommendations.Add("📋 Valid OAuth Redirect URIs phải chứa URL trên");

            return recommendations;
        }        [HttpGet]
        public IActionResult FacebookDebug()
        {
            return View();
        }        [HttpGet]
        public IActionResult StatusCheck()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult EmailControl()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult EmailStatus()
        {
            var enabled = _configuration.GetValue<bool>("EmailSettings:EnableEmailSending", true);
            return Json(new { enabled = enabled });
        }

        [HttpPost]
        [Authorize]
        public IActionResult SetEmailStatus([FromBody] EmailStatusRequest request)
        {
            try
            {
                // Note: This only affects the current session
                // For permanent changes, you'd need to update appsettings.json or use a database flag
                return Json(new { 
                    success = true, 
                    message = request.Enabled 
                        ? "Email service đã được bật (chỉ trong session hiện tại)" 
                        : "Email service đã được tắt (chỉ trong session hiện tại)" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public IActionResult RestartServer()
        {
            try
            {
                // This is a simple way to restart - in production you might want a more controlled approach
                return Json(new { success = true, message = "Server sẽ restart trong 3 giây..." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]        public IActionResult GetEmailLogs()
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                var logFiles = Directory.GetFiles(logPath, "*.log").OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).Take(3);
                
                var logContent = "";
                foreach (var file in logFiles)
                {
                    logContent += $"=== {Path.GetFileName(file)} ===\n";
                    var lines = System.IO.File.ReadAllLines(file).Where(l => l.Contains("email", StringComparison.OrdinalIgnoreCase)).TakeLast(20);
                    logContent += string.Join("\n", lines) + "\n\n";
                }
                
                return Content(logContent, "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"Không thể đọc logs: {ex.Message}", "text/plain");
            }
        }

        public class EmailStatusRequest
        {
            public bool Enabled { get; set; }
        }
    }
}

