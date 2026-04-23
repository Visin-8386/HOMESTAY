using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Security;

namespace WebHS.Middleware
{
    public class AuthorizationExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationExceptionMiddleware> _logger;

        public AuthorizationExceptionMiddleware(RequestDelegate next, ILogger<AuthorizationExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for path: {Path}", context.Request.Path);
                
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                
                if (context.Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    context.Response.ContentType = "application/json";
                    var response = new { error = "Unauthorized access", message = "You don't have permission to access this resource." };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                }
                else
                {
                    context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(context.Request.Path));
                }
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex, "Security exception for path: {Path}", context.Request.Path);
                
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                
                if (context.Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    context.Response.ContentType = "application/json";
                    var response = new { error = "Forbidden", message = "Access denied." };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                }
                else
                {
                    context.Response.Redirect("/Home/AccessDenied");
                }
            }
        }
    }
}