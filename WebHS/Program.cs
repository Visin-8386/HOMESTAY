using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.IdentityModel.Tokens;
using WebHS.Data;
using WebHS.Models;
using WebHS.Services;
using WebHS.Services.Enhanced;
using WebHS.Extensions;
using WebHS.Middleware;
using Serilog;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OfficeOpenXml;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Globalization;

// Register UTF-8 encoding for Vietnamese character support
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webhs-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), 
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
});

// Add Identity with persistent token storage
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings - Relaxed for development
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true; // Require email confirmation

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Token settings for persistent storage
    options.Tokens.EmailConfirmationTokenProvider = "EntityFramework";
    options.Tokens.PasswordResetTokenProvider = "EntityFramework";
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
// Add custom token provider that stores in database
.AddTokenProvider<WebHS.Services.EntityFrameworkTokenProvider<User>>("EntityFramework")
// Add custom user validator to check IsActive status
.AddUserValidator<WebHS.Services.CustomUserValidator>()
// Add custom SignInManager to check IsActive during sign-in
.AddSignInManager<WebHS.Services.CustomSignInManager>();

// Register the entity framework token provider
builder.Services.AddTransient<WebHS.Services.EntityFrameworkTokenProvider<User>>();

// Add external authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        
        // Save tokens to database
        options.SaveTokens = true;
        
        // Request additional scopes if needed
        options.Scope.Add("email");
        options.Scope.Add("profile");
        
        // Request offline access for refresh tokens
        options.AccessType = "offline";
        
        // Handle token received event
        options.Events.OnTicketReceived = context =>
        {
            // Log that tokens are being saved
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("Google tokens received and will be saved for user");
            return Task.CompletedTask;
        };
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";
        
        // Save tokens to database
        options.SaveTokens = true;
        
        // Request additional permissions
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
        
        // Handle token received event
        options.Events.OnTicketReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("Facebook tokens received and will be saved for user");
            return Task.CompletedTask;
        };
    });

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? ""))
        };
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure email settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Configure EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Register health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Register HTTP clients for external services
builder.Services.AddHttpClient<IYouTubeService, YouTubeService>();
builder.Services.AddHttpClient<WebHS.Services.Enhanced.EnhancedGeocodingService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpClient<IUnsplashService, UnsplashService>();
builder.Services.AddHttpClient<IYouTubeService, YouTubeService>();
builder.Services.AddHttpClient<IGoogleReCaptchaService, GoogleReCaptchaService>();

// Register services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IHomestayService, HomestayService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUnsplashService, UnsplashService>();
builder.Services.AddScoped<IGoogleReCaptchaService, GoogleReCaptchaService>();
builder.Services.AddScoped<IGoogleStructuredDataService, GoogleStructuredDataService>();
builder.Services.AddScoped<GeocodingService>();
builder.Services.AddScoped<WebHS.Services.Enhanced.EnhancedGeocodingService>();
builder.Services.AddScoped<WebHS.Services.Enhanced.GoogleGeocodingService>();
builder.Services.AddScoped<VietnamAddressService>();
builder.Services.AddScoped<HybridGeocodingService>();
// Add free geocoding service as a fallback for users without Google Maps API key
builder.Services.AddHttpClient<WebHS.Services.Enhanced.FreeGeocodingService>();
builder.Services.AddScoped<WebHS.Services.Enhanced.FreeGeocodingService>();
// Register the data seeder service
builder.Services.AddScoped<DataSeederServiceFixed>();

// Register new professional services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddScoped<UserTokenService>();
builder.Services.AddScoped<IMessageTemplateService, MessageTemplateService>();

// Register payment services
builder.Services.AddScoped<IPayPalService, PayPalService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();

// Register AI services  
builder.Services.AddHttpClient();
builder.Services.AddTransient<IGeminiService, GeminiService>(); // Sử dụng Gemini thật với model gemini-2.0-flash
// builder.Services.AddTransient<IGeminiService, MockGeminiService>(); // Backup nếu API không hoạt động

// Register messaging service
builder.Services.AddScoped<IMessagingService, MessagingService>();

// Register Excel export service
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

// Register website info service for AI context
builder.Services.AddScoped<IWebsiteInfoService, WebsiteInfoService>();

// Register claim management service
builder.Services.AddScoped<IClaimManagementService, ClaimManagementService>();

// Register background service
// Register background services
builder.Services.AddHostedService<BackgroundJobHostedService>();
builder.Services.AddHostedService<BookingStatusUpdateService>();
builder.Services.AddHostedService<TokenCleanupService>();

// Add localization services for Vietnamese support
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("vi-VN"), new CultureInfo("en-US") };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Add database optimization services
builder.Services.AddDatabaseOptimization(builder.Configuration);
builder.Services.AddDatabaseMaintenance();
builder.Services.AddScoped<DatabaseOptimizationService>();
builder.Services.AddScoped<CachedHomestayRepository>();
builder.Services.AddScoped<DatabasePerformanceMonitor>();

// Add memory cache
builder.Services.AddMemoryCache();

// Register claim-based authorization
builder.Services.AddAuthorization(options =>
{
    // Đăng ký các policy cho quản trị
    options.AddPolicy("can_manage_users", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanManageUsers, "true"));
    options.AddPolicy("can_manage_roles", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanManageRoles, "true"));
    options.AddPolicy("can_view_reports", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanViewReports, "true"));
    options.AddPolicy("can_manage_properties", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanManageProperties, "true"));
    options.AddPolicy("can_approve_listings", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanApproveListings, "true"));
    options.AddPolicy("can_moderate_reviews", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanModerateReviews, "true"));
    options.AddPolicy("can_manage_promotions", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanManagePromotions, "true"));
    options.AddPolicy("can_access_api_keys", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.CanAccessApiKeys, "true"));
    
    // Đăng ký policy cho vai trò host đặc biệt
    options.AddPolicy("super_host", policy => policy.RequireClaim(WebHS.Models.WebHSClaimTypes.SuperHost, "true"));
});

// Đăng ký claim requirement handler
builder.Services.AddSingleton<IAuthorizationHandler, WebHS.Attributes.ClaimRequirementHandler>();

// Register policy providers for dynamic claim policies
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Add global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add email confirmation middleware
app.UseMiddleware<EmailConfirmationMiddleware>();

// Add authorization exception handling
app.UseMiddleware<AuthorizationExceptionMiddleware>();

// Add localization middleware
app.UseRequestLocalization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map health checks
app.MapHealthChecks("/health");

// Seed message templates
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var seeder = new MessageTemplateSeeder(context);
    await seeder.SeedAsync();
}

app.Run();
