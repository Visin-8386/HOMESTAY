namespace WebHS.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            
            // Only apply CSP in production to avoid development issues
            if (!_environment.IsDevelopment())
            {
                var csp = "default-src 'self'; " +
                         "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com https://www.googletagmanager.com https://www.google.com https://pagead2.googlesyndication.com https://www.recaptcha.net https://www.gstatic.com; " +
                         "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://unpkg.com; " +
                         "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                         "img-src 'self' data: https: blob: https://*.tile.openstreetmap.org; " +
                         "frame-src 'self' https://www.youtube.com https://youtube.com; " +
                         "connect-src 'self' https://api.stripe.com https://provinces.open-api.vn https://nominatim.openstreetmap.org https://www.google-analytics.com https://analytics.google.com https://www.googletagmanager.com;";
                
                context.Response.Headers["Content-Security-Policy"] = csp;
            }

            await _next(context);
        }
    }
}
