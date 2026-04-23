using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHS.ViewModels;
using WebHS.Services;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHSPromotionType = WebHS.Models.PromotionType;

namespace WebHS.Services
{    public interface IBookingService
    {
        Task<BookingDetailViewModel?> CreateBookingAsync(BookingViewModel model, string userId);
        Task<BookingListViewModel> GetUserBookingsAsync(string userId, string status = "all", int page = 1);
        Task<HostBookingListViewModel> GetHostBookingsAsync(string hostId, string status = "all", int page = 1);
        Task<BookingDetailViewModel?> GetBookingDetailAsync(int id, string userId);
        Task<bool> CancelBookingAsync(int id, string userId);
        Task<bool> ConfirmBookingAsync(int id);
        Task<decimal> CalculateBookingAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null);
        Task<bool> IsDateAvailableAsync(int homestayId, DateTime checkIn, DateTime checkOut);
        Task<List<DateTime>> GetBookedDatesAsync(int homestayId);
        Task SyncBlockedDatesFromBookingsAsync();
        Task SyncBlockedDatesWithBookingsAsync();        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task UpdateBookingStatusAsync(int bookingId, BookingStatus status);
        Task CreateBlockedDatesForPaidBookingAsync(int bookingId);
        Task CancelExpiredPendingBookingsAsync();
    }

    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;
        private readonly IPricingService _pricingService;
        private const string DefaultPlaceholderImage = "/images/placeholder-homestay.svg";
        
        // Semaphore to prevent race conditions during booking creation
        private static readonly SemaphoreSlim _bookingSemaphore = new SemaphoreSlim(1, 1);

        public BookingService(ApplicationDbContext context, IEmailService emailService, ILogger<BookingService> logger, IPricingService pricingService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<BookingDetailViewModel?> CreateBookingAsync(BookingViewModel model, string userId)
        {
            // Use semaphore to prevent race conditions
            await _bookingSemaphore.WaitAsync();
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _logger.LogInformation("Creating booking for user {UserId}, homestay {HomestayId}", userId, model.HomestayId);

                    // Validate dates
                    if (model.CheckInDate >= model.CheckOutDate || model.CheckInDate < DateTime.Today)
                    {
                        _logger.LogWarning("Invalid booking dates: CheckIn={CheckIn}, CheckOut={CheckOut}", model.CheckInDate, model.CheckOutDate);
                        return null;
                    }

                    // ADDED: Validate minimum stay requirement
                    var numberOfNights = (model.CheckOutDate - model.CheckInDate).Days;
                    if (numberOfNights < 1)
                    {
                        _logger.LogWarning("Booking does not meet minimum stay requirement: {NumberOfNights} nights", numberOfNights);
                        return null;
                    }

                    // Double-check availability within transaction to prevent race conditions
                    var isAvailable = await IsDateAvailableAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);
                    if (!isAvailable)
                    {
                        _logger.LogWarning("Homestay {HomestayId} not available for dates {CheckIn} to {CheckOut}", model.HomestayId, model.CheckInDate, model.CheckOutDate);
                        return null;
                    }

                var homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .Include(h => h.Images)
                    .FirstOrDefaultAsync(h => h.Id == model.HomestayId && h.IsActive && h.IsApproved);

                if (homestay == null)
                {
                    _logger.LogWarning("Homestay {HomestayId} not found or not available", model.HomestayId);
                    return null;
                }

                // Validate guest count
                if (model.NumberOfGuests > homestay.MaxGuests)
                {
                    _logger.LogWarning("Guest count {GuestCount} exceeds max capacity {MaxGuests} for homestay {HomestayId}", 
                        model.NumberOfGuests, homestay.MaxGuests, model.HomestayId);
                    return null;
                }

                // Calculate pricing using PricingService
                var subTotal = await _pricingService.CalculateTotalPriceAsync(model.HomestayId, model.CheckInDate, model.CheckOutDate);

                WebHSPromotion? promotion = null;
                decimal discountAmount = 0;

                if (!string.IsNullOrEmpty(model.PromotionCode))
                {
                    promotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.Code == model.PromotionCode &&
                                                p.IsActive &&
                                                p.StartDate <= DateTime.UtcNow &&
                                                p.EndDate >= DateTime.UtcNow &&
                                                (p.UsageLimit == null || p.UsedCount < p.UsageLimit));

                    if (promotion != null)
                    {
                        discountAmount = promotion.Type == WebHSPromotionType.Percentage
                            ? subTotal * (promotion.Value / 100)
                            : promotion.Value;

                        discountAmount = Math.Min(discountAmount, subTotal);
                        _logger.LogInformation("Applied promotion {PromotionCode} with discount {DiscountAmount}", model.PromotionCode, discountAmount);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid or expired promotion code: {PromotionCode}", model.PromotionCode);
                    }
                }

                var finalAmount = subTotal - discountAmount;

                var booking = new Booking
                {
                    UserId = userId,
                    HomestayId = model.HomestayId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    NumberOfGuests = model.NumberOfGuests,
                    TotalAmount = subTotal,                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    Status = BookingStatus.Pending, // Changed from Paid to Pending
                    Notes = model.Notes,
                    PromotionId = promotion?.Id
                };                _context.Bookings.Add(booking);
                
                // Save booking first to get the ID
                await _context.SaveChangesAsync();

                // IMPORTANT: Do NOT create blocked dates for pending bookings
                // Blocked dates will only be created when payment is completed
                // This prevents locking dates for unpaid bookings
                
                // Update promotion usage count only after successful booking creation
                if (promotion != null)
                {
                    promotion.UsedCount++;
                    _logger.LogInformation("Updated promotion usage count for {PromotionCode}: {UsedCount}/{UsageLimit}", 
                        promotion.Code, promotion.UsedCount, promotion.UsageLimit);
                }

                // Save all changes
                await _context.SaveChangesAsync();                await transaction.CommitAsync();

                _logger.LogInformation("Booking {BookingId} created successfully in pending status for user {UserId}. Blocked dates will be created after payment.", 
                    booking.Id, userId);

                // Send confirmation email to customer and notification to host
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Send detailed booking confirmation to customer
                        await _emailService.SendDetailedBookingConfirmationAsync(user.Email!, booking);
                        _logger.LogInformation("Sent detailed booking confirmation email to customer {Email}", user.Email);
                        
                        // Send booking notification to host
                        if (!string.IsNullOrEmpty(homestay.Host?.Email))
                        {
                            await _emailService.SendBookingNotificationToHostAsync(homestay.Host.Email, booking);
                            _logger.LogInformation("Sent booking notification email to host {Email}", homestay.Host.Email);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for booking {BookingId}", booking.Id);
                    // Don't fail the booking if email fails
                }                return new BookingDetailViewModel
                {
                    Booking = booking,
                    HomestayName = homestay.Name,
                    PrimaryImage = GetImageUrlWithFallback(homestay.Images),
                    HostName = $"{homestay.Host?.FirstName} {homestay.Host?.LastName}",
                    CanReview = false,
                    CanCancel = booking.Status == BookingStatus.Paid,
                    HomestayImage = GetImageUrlWithFallback(homestay.Images)
                };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating booking for user {UserId}, homestay {HomestayId}", userId, model.HomestayId);
                    throw;
                }
            }
            finally
            {
                // Always release the semaphore to prevent deadlocks
                _bookingSemaphore.Release();
            }
        }

        public async Task<BookingListViewModel> GetUserBookingsAsync(string userId, string status = "all", int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.Homestay.Host)
                .Where(b => b.UserId == userId);

            if (status != "all")
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 10;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingDetailViewModel
                {
                    Id = b.Id,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfGuests = b.NumberOfGuests,
                    FinalAmount = b.FinalAmount,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    DiscountAmount = b.DiscountAmount,
                    Booking = b,
                    HomestayName = b.Homestay.Name,
                    HomestayLocation = $"{b.Homestay.City}, {b.Homestay.State}",
                    PrimaryImage = GetImageUrlWithFallback(b.Homestay.Images),
                    HostName = $"{b.Homestay.Host.FirstName} {b.Homestay.Host.LastName}",
                    CanReview = b.Status == BookingStatus.Completed && !b.ReviewRating.HasValue,
                    CanCancel = b.Status == BookingStatus.Paid || b.Status == BookingStatus.Paid,
                    HomestayImage = GetImageUrlWithFallback(b.Homestay.Images)
                })
                .ToListAsync();

            return new BookingListViewModel
            {
                Bookings = bookings,
                Status = status,
                Page = page,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
        }

        public async Task<HostBookingListViewModel> GetHostBookingsAsync(string hostId, string status = "all", int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.User)
                .Where(b => b.Homestay.HostId == hostId);

            if (status != "all")
            {
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    query = query.Where(b => b.Status == bookingStatus);
                }
            }

            var pageSize = 10;
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingDetailViewModel
                {
                    Id = b.Id,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfGuests = b.NumberOfGuests,
                    FinalAmount = b.FinalAmount,
                    Status = b.Status,
                    TotalAmount = b.TotalAmount,
                    DiscountAmount = b.DiscountAmount,
                    Booking = b,
                    HomestayName = b.Homestay.Name,
                    HomestayLocation = $"{b.Homestay.City}, {b.Homestay.State}",
                    PrimaryImage = GetImageUrlWithFallback(b.Homestay.Images),
                    UserName = $"{b.User.FirstName} {b.User.LastName}",
                    UserEmail = b.User.Email ?? "",
                    UserPhone = b.User.PhoneNumber ?? "",
                    CanReview = false, // Host doesn't review guests
                    CanCancel = b.Status == BookingStatus.Paid,
                    HomestayImage = GetImageUrlWithFallback(b.Homestay.Images)
                })
                .ToListAsync();

            return new HostBookingListViewModel
            {
                Bookings = bookings,
                Status = status,
                Page = page,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };
        }

        public async Task<BookingDetailViewModel?> GetBookingDetailAsync(int id, string userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Homestay)
                    .ThenInclude(h => h.Images)
                .Include(b => b.Homestay.Host)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
                return null;

            return new BookingDetailViewModel
            {
                Id = booking.Id,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                Booking = booking,
                HomestayName = booking.Homestay.Name,
                HomestayLocation = $"{booking.Homestay.City}, {booking.Homestay.State}",
                PrimaryImage = GetImageUrlWithFallback(booking.Homestay.Images),
                HostName = $"{booking.Homestay.Host.FirstName} {booking.Homestay.Host.LastName}",
                CanReview = booking.Status == BookingStatus.Completed && !booking.ReviewRating.HasValue,
                CanCancel = booking.Status == BookingStatus.Paid || booking.Status == BookingStatus.Paid,
                HomestayImage = GetImageUrlWithFallback(booking.Homestay.Images)
            };
        }        public async Task<bool> CancelBookingAsync(int id, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

                if (booking == null || (booking.Status != BookingStatus.Paid && booking.Status != BookingStatus.Paid))
                    return false;

                // Update booking status
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;

                // Remove blocked dates associated with this booking
                var blockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == booking.HomestayId && 
                                bd.Date >= booking.CheckInDate.Date && 
                                bd.Date < booking.CheckOutDate.Date &&
                                bd.Reason != null && bd.Reason.Contains($"Booking #{booking.Id}"))
                    .ToListAsync();

                if (blockedDates.Any())
                {
                    _context.BlockedDates.RemoveRange(blockedDates);
                    _logger.LogInformation("Removed {Count} blocked dates for cancelled booking {BookingId}", 
                        blockedDates.Count, booking.Id);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Booking {BookingId} cancelled successfully and blocked dates removed", booking.Id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                throw;
            }
        }

        public async Task<bool> ConfirmBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || booking.Status != BookingStatus.Paid)
                return false;

            booking.Status = BookingStatus.Paid;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateBookingAmount(int homestayId, DateTime checkIn, DateTime checkOut, string? promotionCode = null)
        {
            var homestay = await _context.Homestays.FindAsync(homestayId);
            if (homestay == null)
                return 0;

            // Use PricingService to calculate total amount with dynamic pricing
            var subTotal = await _pricingService.CalculateTotalPriceAsync(homestayId, checkIn, checkOut);

            if (string.IsNullOrEmpty(promotionCode))
                return subTotal;

            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == promotionCode && 
                                        p.IsActive &&
                                        p.StartDate <= DateTime.UtcNow &&
                                        p.EndDate >= DateTime.UtcNow &&
                                        (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value) &&
                                        (!p.MinOrderAmount.HasValue || subTotal >= p.MinOrderAmount.Value));

            if (promotion == null)
                return subTotal;

            var discountAmount = promotion.Type == WebHSPromotionType.Percentage
                ? subTotal * (promotion.Value / 100)
                : promotion.Value;

            if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                discountAmount = promotion.MaxDiscountAmount.Value;

            return subTotal - discountAmount;
        }

        // Enhanced availability checking with comprehensive validation
        public async Task<bool> IsDateAvailableAsync(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                _logger.LogDebug("Checking availability for homestay {HomestayId} from {CheckIn} to {CheckOut}", homestayId, checkIn, checkOut);

                // Basic validation - prevent same-day bookings
                if (checkIn >= checkOut)
                {
                    _logger.LogDebug("Invalid date range: CheckIn {CheckIn} must be before CheckOut {CheckOut}", checkIn, checkOut);
                    return false;
                }

                // Prevent past bookings
                if (checkIn < DateTime.Today)
                {
                    _logger.LogDebug("CheckIn date {CheckIn} is in the past", checkIn);
                    return false;
                }        // Check for overlapping bookings - Since we now block checkout dates too, we need stricter checking
        // If we allow same-day checkout/checkin: checkIn < b.CheckOutDate && checkOut > b.CheckInDate
        // If we block checkout day completely: checkIn <= b.CheckOutDate && checkOut >= b.CheckInDate
        var hasConflictingBooking = await _context.Bookings
            .AnyAsync(b => b.HomestayId == homestayId &&
                         (b.Status == BookingStatus.Paid || 
                          b.Status == BookingStatus.Paid ||
                          b.Status == BookingStatus.Completed) &&
                         checkIn <= b.CheckOutDate && checkOut >= b.CheckInDate);

                if (hasConflictingBooking)
                {
                    _logger.LogDebug("Homestay {HomestayId} has conflicting booking for requested dates", homestayId);
                    return false;
                }                // Check for blocked dates - Include checkout date since we now block it
                var hasBlockedDates = await _context.BlockedDates
                    .AnyAsync(bd => bd.HomestayId == homestayId &&
                                  bd.Date >= checkIn && bd.Date <= checkOut);

                if (hasBlockedDates)
                {
                    _logger.LogDebug("Homestay {HomestayId} has blocked dates in requested range", homestayId);
                    return false;
                }

                _logger.LogDebug("Homestay {HomestayId} is available for requested dates", homestayId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for homestay {HomestayId}", homestayId);
                return false;
            }
        }        public async Task<List<DateTime>> GetBookedDatesAsync(int homestayId)
        {
            try
            {
                _logger.LogInformation("Getting booked dates for homestay {HomestayId}", homestayId);
                _logger.LogInformation("Today is: {Today}", DateTime.Today);
                
                // Get blocked dates directly from BlockedDates table with EF query
                var blockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == homestayId && bd.Date >= DateTime.Today)
                    .Select(bd => bd.Date)
                    .OrderBy(d => d)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} blocked dates for homestay {HomestayId}", blockedDates.Count, homestayId);
                
                // Log each blocked date for debugging
                foreach (var date in blockedDates)
                {
                    _logger.LogInformation("Blocked date: {Date}", date.ToString("yyyy-MM-dd"));
                }
                
                // Also get dates from active bookings as fallback (in case BlockedDates are not populated)
                var bookingDates = await _context.Bookings
                    .Where(b => b.HomestayId == homestayId && 
                               (b.Status == BookingStatus.Paid || 
                                b.Status == BookingStatus.Completed))
                    .ToListAsync();

                var bookingDatesList = new List<DateTime>();
                foreach (var booking in bookingDates)
                {
                    var currentDate = booking.CheckInDate.Date;
                    while (currentDate < booking.CheckOutDate.Date)
                    {
                        bookingDatesList.Add(currentDate);
                        currentDate = currentDate.AddDays(1);
                    }
                }

                // Combine both sources and remove duplicates
                var allBookedDates = blockedDates.Concat(bookingDatesList)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} total booked dates for homestay {HomestayId} ({BlockedCount} from BlockedDates, {BookingCount} from Bookings)", 
                    allBookedDates.Count, homestayId, blockedDates.Count, bookingDatesList.Count);
                
                return allBookedDates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booked dates for homestay {HomestayId}", homestayId);
                return new List<DateTime>();
            }
        }

        // Method to sync BlockedDates from existing bookings
        public async Task SyncBlockedDatesFromBookingsAsync()
        {
            try
            {
                _logger.LogInformation("Starting sync of BlockedDates from existing bookings...");
                
                // Get all active bookings
                var activeBookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Completed)
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} active bookings to sync", activeBookings.Count);
                
                var blockedDatesToAdd = new List<BlockedDate>();
                
                foreach (var booking in activeBookings)
                {
                    var currentDate = booking.CheckInDate.Date;
                    while (currentDate < booking.CheckOutDate.Date)
                    {
                        // Check if blocked date already exists
                        var existingBlocked = await _context.BlockedDates
                            .FirstOrDefaultAsync(bd => bd.HomestayId == booking.HomestayId && bd.Date == currentDate);
                        
                        if (existingBlocked == null)
                        {
                            blockedDatesToAdd.Add(new BlockedDate
                            {
                                HomestayId = booking.HomestayId,
                                Date = currentDate,
                                Reason = $"Booking #{booking.Id} - {booking.NumberOfGuests} guests",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        
                        currentDate = currentDate.AddDays(1);
                    }
                }
                
                if (blockedDatesToAdd.Any())
                {
                    _context.BlockedDates.AddRange(blockedDatesToAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {Count} blocked dates from existing bookings", blockedDatesToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("No new blocked dates to add - all are already synced");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing blocked dates from bookings");
                throw;
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Homestay)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }        public async Task UpdateBookingStatusAsync(int bookingId, BookingStatus status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return;

                var oldStatus = booking.Status;
                booking.Status = status;
                booking.UpdatedAt = DateTime.UtcNow;                // Handle blocked dates based on status change
                if (status == BookingStatus.Cancelled && 
                    (oldStatus == BookingStatus.Paid || oldStatus == BookingStatus.Pending))
                {
                    // Remove blocked dates when booking is cancelled
                    var blockedDates = await _context.BlockedDates
                        .Where(bd => bd.HomestayId == booking.HomestayId && 
                                    bd.Date >= booking.CheckInDate.Date && 
                                    bd.Date < booking.CheckOutDate.Date &&
                                    bd.Reason != null && bd.Reason.Contains($"Booking #{booking.Id}"))
                        .ToListAsync();

                    if (blockedDates.Any())
                    {
                        _context.BlockedDates.RemoveRange(blockedDates);
                        _logger.LogInformation("Removed {Count} blocked dates for cancelled booking {BookingId}", 
                            blockedDates.Count, bookingId);
                    }
                }
                else if ((status == BookingStatus.Paid || status == BookingStatus.Pending) && 
                         oldStatus == BookingStatus.Cancelled)
                {
                    // Re-create blocked dates when booking is reactivated from cancelled status
                    var existingBlockedDates = await _context.BlockedDates
                        .Where(bd => bd.HomestayId == booking.HomestayId && 
                                    bd.Date >= booking.CheckInDate.Date && 
                                    bd.Date < booking.CheckOutDate.Date)
                        .Select(bd => bd.Date.Date)
                        .ToListAsync();

                    var blockedDates = new List<BlockedDate>();
                    var currentDate = booking.CheckInDate.Date;
                    while (currentDate < booking.CheckOutDate.Date)
                    {
                        // Only add if not already blocked
                        if (!existingBlockedDates.Contains(currentDate))
                        {
                            blockedDates.Add(new BlockedDate
                            {
                                HomestayId = booking.HomestayId,
                                Date = currentDate,
                                Reason = $"Booking #{booking.Id} - Reactivated",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        currentDate = currentDate.AddDays(1);
                    }

                    if (blockedDates.Any())
                    {
                        _context.BlockedDates.AddRange(blockedDates);
                        _logger.LogInformation("Created {Count} blocked dates for reactivated booking {BookingId}", 
                            blockedDates.Count, bookingId);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated booking {BookingId} status from {OldStatus} to {NewStatus}", 
                    bookingId, oldStatus, status);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating booking {BookingId} status to {Status}", bookingId, status);
                throw;
            }
        }

        /// <summary>
        /// Cancels unpaid bookings that have exceeded the timeout period (30 minutes)
        /// </summary>
        public async Task CancelExpiredPendingBookingsAsync()
        {
            var timeoutMinutes = 30;
            var cutoffTime = DateTime.Now.AddMinutes(-timeoutMinutes);
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var expiredBookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Pending && 
                               b.CreatedAt < cutoffTime)
                    .ToListAsync();

                if (expiredBookings.Any())
                {
                    foreach (var booking in expiredBookings)
                    {
                        booking.Status = BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.Now;
                        
                        _logger.LogInformation("Auto-cancelled expired booking {BookingId} created at {CreatedAt}", 
                            booking.Id, booking.CreatedAt);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Auto-cancelled {Count} expired pending bookings", expiredBookings.Count);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling expired pending bookings");
                throw;
            }
        }

        // Sync blocked dates with existing bookings
        public async Task SyncBlockedDatesWithBookingsAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting sync of blocked dates with existing bookings");

                // Get all active bookings (Paid and Pending)
                var activeBookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Paid || b.Status == BookingStatus.Pending)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active bookings to sync", activeBookings.Count);

                // Remove all existing booking-related blocked dates
                var existingBookingBlockedDates = await _context.BlockedDates
                    .Where(bd => bd.Reason != null && bd.Reason.Contains("Booking #"))
                    .ToListAsync();

                if (existingBookingBlockedDates.Any())
                {
                    _context.BlockedDates.RemoveRange(existingBookingBlockedDates);
                    _logger.LogInformation("Removed {Count} existing booking-related blocked dates", existingBookingBlockedDates.Count);
                }                // Create new blocked dates for all active bookings
                var newBlockedDates = new List<BlockedDate>();
                foreach (var booking in activeBookings)
                {
                    var currentDate = booking.CheckInDate.Date;
                    while (currentDate <= booking.CheckOutDate.Date) // Include checkout date
                    {
                        newBlockedDates.Add(new BlockedDate
                        {
                            HomestayId = booking.HomestayId,
                            Date = currentDate,
                            Reason = $"Booking #{booking.Id} - {booking.NumberOfGuests} guests",
                            CreatedAt = DateTime.UtcNow
                        });
                        currentDate = currentDate.AddDays(1);
                    }
                }

                if (newBlockedDates.Any())
                {
                    _context.BlockedDates.AddRange(newBlockedDates);
                    _logger.LogInformation("Created {Count} new blocked dates for active bookings", newBlockedDates.Count);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully synced blocked dates with {BookingCount} active bookings, created {BlockedDatesCount} blocked dates", 
                    activeBookings.Count, newBlockedDates.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error syncing blocked dates with bookings");
                throw;
            }
        }

        /// <summary>
        /// Creates blocked dates for a paid booking to prevent double booking
        /// </summary>
        public async Task CreateBlockedDatesForPaidBookingAsync(int bookingId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId && b.Status == BookingStatus.Paid);
                
                if (booking == null)
                {
                    _logger.LogWarning("Cannot create blocked dates: Booking {BookingId} not found or not paid", bookingId);
                    return;
                }

                // Check if blocked dates already exist for this booking
                var existingBlockedDates = await _context.BlockedDates
                    .Where(bd => bd.HomestayId == booking.HomestayId && 
                                bd.Date >= booking.CheckInDate.Date && 
                                bd.Date <= booking.CheckOutDate.Date &&
                                !string.IsNullOrEmpty(bd.Reason) && 
                                bd.Reason.Contains($"Booking #{booking.Id}"))
                    .ToListAsync();

                if (existingBlockedDates.Any())
                {
                    _logger.LogInformation("Blocked dates already exist for booking {BookingId}", bookingId);
                    return;
                }

                // Create blocked dates for all days in the booking period
                var blockedDates = new List<BlockedDate>();
                var currentDate = booking.CheckInDate.Date;
                while (currentDate <= booking.CheckOutDate.Date)
                {
                    blockedDates.Add(new BlockedDate
                    {
                        HomestayId = booking.HomestayId,
                        Date = currentDate,
                        Reason = $"Booking #{booking.Id} - {booking.NumberOfGuests} guests",
                        CreatedAt = DateTime.UtcNow
                    });
                    currentDate = currentDate.AddDays(1);
                }

                if (blockedDates.Any())
                {
                    _context.BlockedDates.AddRange(blockedDates);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Created {Count} blocked dates for paid booking {BookingId} from {CheckIn} to {CheckOut}", 
                        blockedDates.Count, bookingId, booking.CheckInDate, booking.CheckOutDate);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating blocked dates for booking {BookingId}", bookingId);
                throw;
            }
        }

        // Helper method for centralized image URL handling with fallback
        private static string GetImageUrlWithFallback(IEnumerable<HomestayImage> images)
        {
            var primaryImage = images.FirstOrDefault(i => i.IsPrimary);
            if (primaryImage != null)
                return primaryImage.ImageUrl;

            var firstImage = images.FirstOrDefault();
            if (firstImage != null)
                return firstImage.ImageUrl;

            return "/images/placeholder-homestay.svg"; // Use constant instead of instance field
        }
    }
}

