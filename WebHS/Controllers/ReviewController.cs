using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Services;
using WebHS.ViewModels;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSUser = WebHS.Models.User;

namespace WebHS.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<WebHSUser> _userManager;
        private readonly IEmailService _emailService;

        public ReviewController(ApplicationDbContext context, UserManager<WebHSUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }        [HttpGet]
        public async Task<IActionResult> HomestayReviews(int homestayId, int page = 1)
        {
            var pageSize = 10;
            var skip = (page - 1) * pageSize;

            var homestay = await _context.Homestays
                .Include(h => h.Host)
                .FirstOrDefaultAsync(h => h.Id == homestayId);

            if (homestay == null)
                return NotFound();

            var reviewBookingsQuery = _context.Bookings
                .Include(b => b.User)
                .Where(b => b.HomestayId == homestayId && b.ReviewRating.HasValue && b.ReviewIsActive)
                .OrderByDescending(b => b.ReviewCreatedAt);

            var totalReviews = await reviewBookingsQuery.CountAsync();
            var reviews = await reviewBookingsQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(b => new ReviewViewModel
                {
                    Id = b.Id,
                    UserName = b.User.FullName,
                    UserAvatar = b.User.ProfilePicture,
                    Rating = b.ReviewRating!.Value,
                    Comment = b.ReviewComment ?? string.Empty,
                    CreatedAt = b.ReviewCreatedAt!.Value,
                    BookingId = b.Id
                })
                .ToListAsync();

            var averageRating = await _context.Bookings
                .Where(b => b.HomestayId == homestayId && b.ReviewRating.HasValue && b.ReviewIsActive)
                .AverageAsync(b => (double?)b.ReviewRating) ?? 0;

            var ratingDistribution = await _context.Bookings
                .Where(b => b.HomestayId == homestayId && b.ReviewRating.HasValue && b.ReviewIsActive)
                .GroupBy(b => b.ReviewRating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Rating)
                .ToListAsync();

            var model = new HomestayReviewListViewModel
            {
                HomestayId = homestayId,
                HomestayName = homestay.Name,
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize),
                TotalReviews = totalReviews,
                AverageRating = averageRating,
                RatingDistribution = ratingDistribution.ToDictionary(x => x.Rating!.Value, x => x.Count)
            };

            return View(model);
        }        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId && b.Status == BookingStatus.Completed);

            if (booking == null)
                return NotFound("Booking not found or not eligible for review");

            // Check if review already exists
            if (booking.ReviewRating.HasValue)
                return RedirectToAction("Edit", new { id = bookingId });

            var model = new CreateReviewViewModel
            {
                BookingId = bookingId,
                HomestayId = booking.HomestayId,
                HomestayName = booking.Homestay.Name,
                HomestayImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate
            };

            return View(model);
        }        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReviewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .ThenInclude(h => h.Host)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.UserId == userId && b.Status == BookingStatus.Completed);

            if (booking == null)
            {
                ModelState.AddModelError("", "Booking not found or not eligible for review");
                return View(model);
            }

            // Check if review already exists
            if (booking.ReviewRating.HasValue)
            {
                ModelState.AddModelError("", "Review already exists for this booking");
                return View(model);
            }

            // Add review fields to the booking
            booking.ReviewRating = model.Rating;
            booking.ReviewComment = model.Comment;
            booking.ReviewIsActive = true;
            booking.ReviewCreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send notification email to host
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !string.IsNullOrEmpty(booking.Homestay.Host.Email))
            {
                await _emailService.SendEmailAsync(
                    booking.Homestay.Host.Email,
                    "Bạn có đánh giá mới",
                    $"Homestay '{booking.Homestay.Name}' của bạn đã nhận được đánh giá {model.Rating} sao từ {user.FullName}."
                );
            }

            TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá! Đánh giá của bạn đã được gửi thành công.";
            return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.ReviewRating.HasValue);

            if (booking == null)
                return NotFound();

            var model = new EditReviewViewModel
            {
                Id = booking.Id,
                BookingId = booking.Id,
                HomestayId = booking.HomestayId,
                HomestayName = booking.Homestay.Name,
                HomestayImage = booking.Homestay.Images.FirstOrDefault()?.ImageUrl,
                Rating = booking.ReviewRating!.Value,
                Comment = booking.ReviewComment ?? string.Empty,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate
            };

            return View(model);
        }        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditReviewViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == model.Id && b.UserId == userId && b.ReviewRating.HasValue);

            if (booking == null)
                return NotFound();

            booking.ReviewRating = model.Rating;
            booking.ReviewComment = model.Comment;
            booking.ReviewUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đánh giá của bạn đã được cập nhật thành công.";
            return RedirectToAction("Details", "Booking", new { id = model.BookingId });
        }        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId && b.ReviewRating.HasValue);

            if (booking == null)
                return Json(new { success = false, message = "Review not found" });

            booking.ReviewIsActive = false;
            booking.ReviewUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review deleted successfully" });
        }        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDelete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null || !booking.ReviewRating.HasValue)
                return Json(new { success = false, message = "Review not found" });

            booking.ReviewIsActive = false;
            booking.ReviewUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review deleted successfully" });
        }        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReviews(int page = 1, string search = "")
        {
            var pageSize = 20;
            var skip = (page - 1) * pageSize;

            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                .Where(b => b.ReviewRating.HasValue)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.User.FullName.Contains(search) ||
                                       b.Homestay.Name.Contains(search) ||
                                       (b.ReviewComment != null && b.ReviewComment.Contains(search)));
            }

            var totalReviews = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(b => b.ReviewCreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(b => new AdminReviewViewModel
                {
                    Id = b.Id,
                    UserName = b.User.FullName,
                    HomestayName = b.Homestay.Name,
                    Rating = b.ReviewRating!.Value,
                    Comment = b.ReviewComment ?? string.Empty,
                    IsActive = b.ReviewIsActive,
                    CreatedAt = b.ReviewCreatedAt!.Value
                })
                .ToListAsync();

            var model = new AdminReviewListViewModel
            {
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize),
                TotalReviews = totalReviews,
                SearchTerm = search
            };

            return View(model);
        }        [HttpGet]
        public async Task<IActionResult> AllReviews(int page = 1, string search = "", int? rating = null, string sortBy = "newest")
        {
            var pageSize = 12;
            var skip = (page - 1) * pageSize;

            // Base query for bookings with reviews
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Host)
                .Where(b => b.ReviewRating.HasValue && b.ReviewIsActive);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => 
                    b.Homestay.Name.Contains(search) ||
                    b.ReviewComment!.Contains(search) ||
                    b.User.FirstName.Contains(search) ||
                    b.User.LastName.Contains(search));
            }

            // Apply rating filter
            if (rating.HasValue)
            {
                query = query.Where(b => b.ReviewRating == rating.Value);
            }

            // Apply sorting
            query = sortBy switch
            {
                "oldest" => query.OrderBy(b => b.ReviewCreatedAt),
                "highest" => query.OrderByDescending(b => b.ReviewRating).ThenByDescending(b => b.ReviewCreatedAt),
                "lowest" => query.OrderBy(b => b.ReviewRating).ThenByDescending(b => b.ReviewCreatedAt),
                _ => query.OrderByDescending(b => b.ReviewCreatedAt) // newest
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var bookings = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var reviews = bookings.Select(b => new ReviewItemViewModel
            {
                BookingId = b.Id,
                HomestayId = b.HomestayId,
                HomestayName = b.Homestay.Name,
                HomestayLocation = $"{b.Homestay.City}, {b.Homestay.State}",
                HomestayImage = b.Homestay.Images.FirstOrDefault()?.ImageUrl ?? "/images/default-homestay.jpg",
                UserName = $"{b.User.FirstName} {b.User.LastName}",
                UserInitials = $"{b.User.FirstName.FirstOrDefault()}{b.User.LastName.FirstOrDefault()}",
                Rating = b.ReviewRating!.Value,
                Comment = b.ReviewComment ?? "",
                CreatedAt = b.ReviewCreatedAt!.Value,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                FormattedCreatedAt = b.ReviewCreatedAt!.Value.ToString("dd/MM/yyyy HH:mm"),
                RatingStars = new string('★', b.ReviewRating!.Value) + new string('☆', 5 - b.ReviewRating!.Value),
                IsRecentReview = (DateTime.UtcNow - b.ReviewCreatedAt!.Value).TotalDays <= 7,
                HostName = $"{b.Homestay.Host.FirstName} {b.Homestay.Host.LastName}"
            }).ToList();

            var model = new ReviewListViewModel
            {
                Reviews = reviews,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                SearchTerm = search,
                FilterRating = rating,
                SortBy = sortBy
            };

            return View(model);
        }
    }
}


