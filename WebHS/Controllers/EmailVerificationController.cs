using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebHS.Models;
using WebHS.Services;

namespace WebHS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailVerificationController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerificationController> _logger;

        public EmailVerificationController(
            UserManager<User> userManager,
            IEmailService emailService,
            ILogger<EmailVerificationController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetVerificationStatus()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                IsEmailConfirmed = user.EmailConfirmed,
                Email = user.Email,
                UserId = user.Id
            });
        }

        [HttpPost("resend")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { Message = "Email is required" });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return Ok(new { Message = "If the email exists, a confirmation email has been sent." });
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return Ok(new { Message = "Email is already confirmed" });
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink!);
                
                _logger.LogInformation("Confirmation email resent to {Email}", user.Email);
                
                return Ok(new { Message = "Confirmation email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email for {Email}", request.Email);
                return StatusCode(500, new { Message = "Failed to send confirmation email" });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { Message = "UserId and Token are required" });
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user {UserId}", user.Id);
                return Ok(new { Message = "Email confirmed successfully" });
            }
            else
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Message = "Failed to confirm email", Errors = errors });
            }
        }
    }

    public class ResendEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
