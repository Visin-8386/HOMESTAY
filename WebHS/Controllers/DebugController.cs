using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using WebHS.Models;
using System.Security.Claims;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    public class DebugController : Controller
    {
        private readonly ILogger<DebugController> _logger;
        private readonly SignInManager<WebHSUser> _signInManager;
        private readonly IConfiguration _configuration;

        public DebugController(
            ILogger<DebugController> logger, 
            SignInManager<WebHSUser> signInManager,
            IConfiguration configuration)
        {
            _logger = logger;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public IActionResult GoogleOAuthDebug()
        {
            var viewModel = new GoogleOAuthDebugViewModel
            {
                ClientId = _configuration["Authentication:Google:ClientId"],
                ClientSecret = _configuration["Authentication:Google:ClientSecret"]?.Substring(0, Math.Min(10, _configuration["Authentication:Google:ClientSecret"]?.Length ?? 0)) + "***",
                CurrentUrl = $"{Request.Scheme}://{Request.Host}",
                ExpectedRedirectUri = $"{Request.Scheme}://{Request.Host}/signin-google"
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> TestGoogleCallback()
        {
            try
            {
                _logger.LogInformation("TestGoogleCallback: Starting Google OAuth test");
                
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    _logger.LogError("TestGoogleCallback: GetExternalLoginInfoAsync returned null");
                    
                    // Try to get authentication result directly
                    var authResult = await HttpContext.AuthenticateAsync("Google");
                    if (authResult?.Succeeded == true)
                    {
                        _logger.LogInformation("TestGoogleCallback: Direct authentication succeeded");
                        var directClaims = authResult.Principal.Claims.Select(c => new { c.Type, c.Value }).ToList();
                        return Json(new { 
                            Success = true, 
                            Message = "Direct authentication worked", 
                            Claims = directClaims 
                        });
                    }
                    else
                    {
                        _logger.LogError("TestGoogleCallback: Direct authentication also failed: {Error}", authResult?.Failure?.Message);
                        return Json(new { 
                            Success = false, 
                            Message = "Both GetExternalLoginInfoAsync and direct authentication failed",
                            Error = authResult?.Failure?.Message
                        });
                    }
                }

                _logger.LogInformation("TestGoogleCallback: Successfully got external login info");
                
                var claims = info.Principal.Claims.Select(c => new { c.Type, c.Value }).ToList();
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                
                return Json(new { 
                    Success = true,
                    Provider = info.LoginProvider,
                    Email = email,
                    Claims = claims
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TestGoogleCallback: Exception occurred");
                return Json(new { 
                    Success = false, 
                    Message = "Exception occurred", 
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public IActionResult InitiateGoogleLogin()
        {
            try
            {
                _logger.LogInformation("InitiateGoogleLogin: Starting Google OAuth");
                
                var redirectUrl = Url.Action(nameof(TestGoogleCallback), "Debug");
                _logger.LogInformation("InitiateGoogleLogin: Redirect URL: {RedirectUrl}", redirectUrl);
                
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
                
                return Challenge(properties, "Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InitiateGoogleLogin: Exception occurred");
                return Json(new { 
                    Success = false, 
                    Message = "Exception occurred", 
                    Error = ex.Message 
                });
            }
        }
    }

    public class GoogleOAuthDebugViewModel
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? CurrentUrl { get; set; }
        public string? ExpectedRedirectUri { get; set; }
    }
}
