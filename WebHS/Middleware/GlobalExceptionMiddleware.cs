using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace WebHS.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            try
            {
                // Check if response has already started
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response has already started, cannot send error response.");
                    return;
                }
                
                // Check if API/Ajax request
                bool isApiRequest = context.Request.Path.StartsWithSegments("/api") || 
                                  (context.Request.Headers.ContainsKey("X-Requested-With") && 
                                   context.Request.Headers["X-Requested-With"] == "XMLHttpRequest");
                
                if (isApiRequest)
                {
                    // Handle API/Ajax requests with JSON
                    context.Response.ContentType = "application/json";
                    var response = context.Response;

                    var errorResponse = new ErrorResponse
                    {
                        Success = false,
                        Message = "Đã xảy ra lỗi trong quá trình xử lý yêu cầu"
                    };

                    switch (exception)
                    {
                        case ArgumentException ex:
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            errorResponse.Message = ex.Message;
                            break;
                        
                        case KeyNotFoundException ex:
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                            errorResponse.Message = "Không tìm thấy tài nguyên yêu cầu";
                            break;
                        
                        case UnauthorizedAccessException ex:
                            response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            errorResponse.Message = "Bạn không có quyền truy cập tài nguyên này";
                            break;
                        
                        case InvalidOperationException ex:
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            errorResponse.Message = "Thao tác không hợp lệ";
                            break;
                        
                        default:
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            errorResponse.Message = "Đã xảy ra lỗi hệ thống";
                            break;
                    }

                    // Include detailed error information in development
                    if (_environment.IsDevelopment())
                    {
                        errorResponse.Details = exception.Message;
                        errorResponse.StackTrace = exception.StackTrace;
                    }

                    var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    });
                    
                    await response.WriteAsync(jsonResponse);
                }
                else
                {
                    // For regular HTML requests, redirect to error page
                    string errorMessage = Uri.EscapeDataString(exception.Message);
                    context.Response.Redirect($"/Home/Error?message={errorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions that occur in the exception handler itself
                _logger.LogError(ex, "Error occurred in the exception handler itself");
            }
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
    }

    // Extension method for easy registration
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
