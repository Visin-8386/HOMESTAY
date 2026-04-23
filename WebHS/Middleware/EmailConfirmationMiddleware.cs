using Microsoft.AspNetCore.Identity;
using WebHS.Models;

namespace WebHS.Middleware
{
    public class EmailConfirmationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmailConfirmationMiddleware> _logger;

        public EmailConfirmationMiddleware(RequestDelegate next, ILogger<EmailConfirmationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            // Skip for certain paths
            var path = context.Request.Path.Value?.ToLower();
            var skipPaths = new[]
            {
                "/account/login",
                "/account/register", 
                "/account/logout",
                "/account/confirmemail",
                "/account/checkyouremail",
                "/account/resendconfirmationemail",
                "/account/emailverification",
                "/account/forgotpassword",
                "/account/forgotpasswordconfirmation",
                "/account/resetpassword",
                "/api/",
                "/swagger",
                "/health",
                "/_framework",
                "/css",
                "/js",
                "/images",
                "/favicon.ico"
            };

            if (skipPaths.Any(sp => path?.StartsWith(sp) == true))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null && !user.EmailConfirmed)
                {
                    // Only redirect if not already on email confirmation related pages
                    if (!path?.Contains("checkyouremail") == true && 
                        !path?.Contains("emailverification") == true)
                    {
                        _logger.LogInformation("User {UserId} has unconfirmed email, redirecting to email verification", user.Id);
                        
                        // Sign out user to prevent access to protected resources
                        await signInManager.SignOutAsync();
                        
                        // Redirect to check email page
                        context.Response.Redirect("/Account/CheckYourEmail");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class EmailConfirmationMiddlewareExtensions
    {
        public static IApplicationBuilder UseEmailConfirmation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EmailConfirmationMiddleware>();
        }
    }
}
