using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHS.ViewModels;
using System.Linq; // for LINQ extension methods
using WebHSUser = WebHS.Models.User;

namespace WebHS.Services
{
    public interface IHomestayService
    {
        Task<HomestayListViewModel> SearchHomestaysAsync(HomestaySearchViewModel searchModel);
        Task<HomestayDetailViewModel?> GetHomestayDetailAsync(int id, string? userId = null);
        Task<Homestay?> GetHomestayByIdAsync(int id);
        Task<int> CreateHomestayAsync(CreateHomestayViewModel model, string hostId);
        Task<bool> UpdateHomestayAsync(EditHomestayViewModel model, string hostId);
        Task<bool> DeleteHomestayAsync(int id, string hostId);
        Task<IEnumerable<HomestayCardViewModel>> GetPopularHomestaysAsync(int count = 8);
        Task<IEnumerable<HomestayCardViewModel>> GetHostHomestaysAsync(string hostId);
        Task<bool> ApproveHomestayAsync(int id);
        Task<bool> RejectHomestayAsync(int id);
        Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address, string city, string state);
    }

    public class HomestayService : IHomestayService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<HomestayService> _logger;
        private readonly GeocodingService _geocodingService;
        private const string DefaultPlaceholderImage = "/images/placeholder-homestay.svg";

        public HomestayService(ApplicationDbContext context, IFileUploadService fileUploadService, ILogger<HomestayService> logger, GeocodingService geocodingService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
            _geocodingService = geocodingService;
        }

        public async Task<HomestayListViewModel> SearchHomestaysAsync(HomestaySearchViewModel searchModel)
        {
            var query = _context.Homestays
                .Include(h => h.Images)
                .Include(h => h.Bookings.Where(b => b.ReviewRating.HasValue))
                .Where(h => h.IsActive && h.IsApproved);

            // Filter by location
            if (!string.IsNullOrEmpty(searchModel.Location))
            {
                query = query.Where(h => h.City.Contains(searchModel.Location) ||
                                       h.State.Contains(searchModel.Location) ||
                                       h.Address.Contains(searchModel.Location));
            }

            // Filter by price range
            if (searchModel.MinPrice.HasValue)
                query = query.Where(h => h.PricePerNight >= searchModel.MinPrice.Value);

            if (searchModel.MaxPrice.HasValue)
                query = query.Where(h => h.PricePerNight <= searchModel.MaxPrice.Value);

            // Filter by guests
            if (searchModel.Guests > 0)
                query = query.Where(h => h.MaxGuests >= searchModel.Guests);

            // Filter by bedrooms
            if (searchModel.Bedrooms.HasValue)
                query = query.Where(h => h.Bedrooms >= searchModel.Bedrooms.Value);

            // Filter by bathrooms
            if (searchModel.Bathrooms.HasValue)
                query = query.Where(h => h.Bathrooms >= searchModel.Bathrooms.Value);

            // Filter by amenities
            if (searchModel.AmenityIds != null && searchModel.AmenityIds.Any())
            {
                query = query.Where(h => h.HomestayAmenities
                    .Any(ha => searchModel.AmenityIds.Contains(ha.AmenityId)));
            }

            // Filter by availability
            if (searchModel.CheckInDate.HasValue && searchModel.CheckOutDate.HasValue)
            {
                query = query.Where(h => !h.Bookings.Any(b =>
                    (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Paid) &&
                    ((searchModel.CheckInDate.Value < b.CheckOutDate && searchModel.CheckOutDate.Value > b.CheckInDate))))
                    .Where(h => !h.BlockedDates.Any(bd =>
                        bd.Date >= searchModel.CheckInDate.Value && bd.Date < searchModel.CheckOutDate.Value));
            }

            // Apply sorting
            query = searchModel.SortBy switch
            {
                "price_low" => query.OrderBy(h => h.PricePerNight),
                "price_high" => query.OrderByDescending(h => h.PricePerNight),
                "rating" => query.OrderByDescending(h => h.AverageRating),
                _ => query.OrderByDescending(h => h.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)searchModel.PageSize);

            var homestays = await query
                .Skip((searchModel.Page - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .ToListAsync();

            var now = DateTime.Now;
            var homestayViewModels = new List<HomestayCardViewModel>();
            
            foreach (var h in homestays)
            {
                // Lấy thông tin về booking hiện tại và sắp tới của homestay
                var currentAndUpcomingBookings = await _context.Bookings
                    .Where(b => b.HomestayId == h.Id && 
                           (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Paid) &&
                           b.CheckOutDate > now)
                    .OrderBy(b => b.CheckInDate)
                    .ToListAsync();
                
                var bookingPeriods = currentAndUpcomingBookings
                    .Select(b => new BookingPeriod { 
                        CheckInDate = b.CheckInDate, 
                        CheckOutDate = b.CheckOutDate 
                    })
                    .ToList();
                
                // Xác định homestay có đang được đặt không
                var isCurrentlyBooked = currentAndUpcomingBookings.Any(b => 
                    b.CheckInDate <= now && b.CheckOutDate > now);
                
                // Tìm ngày tiếp theo khả dụng
                DateTime? nextAvailableDate = null;
                if (isCurrentlyBooked && currentAndUpcomingBookings.Any())
                {
                    var currentBooking = currentAndUpcomingBookings
                        .FirstOrDefault(b => b.CheckInDate <= now && b.CheckOutDate > now);
                    
                    if (currentBooking != null)
                    {
                        nextAvailableDate = currentBooking.CheckOutDate;
                    }
                }
                  homestayViewModels.Add(new HomestayCardViewModel
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    PricePerNight = h.PricePerNight,
                    PrimaryImage = GetImageUrlWithFallback(h.Images),
                    AverageRating = h.AverageRating,
                    ReviewCount = h.ReviewCount,
                    MaxGuests = h.MaxGuests,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    IsActive = h.IsActive,
                    IsApproved = h.IsApproved,
                    YouTubeVideoId = h.YouTubeVideoId,
                    // Thông tin tình trạng đặt phòng
                    IsBooked = isCurrentlyBooked,
                    NextAvailableDate = nextAvailableDate,
                    BookingPeriods = bookingPeriods
                });
            }

            var availableAmenities = await _context.Amenities
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            return new HomestayListViewModel
            {
                Homestays = homestayViewModels,
                Search = searchModel,
                AvailableAmenities = availableAmenities,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<HomestayDetailViewModel?> GetHomestayDetailAsync(int id, string? userId = null)
        {
            Homestay? homestay;
            if (!string.IsNullOrEmpty(userId))
            {
                // Nếu là host của homestay thì lấy cả chưa duyệt
                homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .Include(h => h.Images)
                    .Include(h => h.HomestayAmenities).ThenInclude(ha => ha.Amenity)
                    .Include(h => h.Bookings.Where(b => b.ReviewRating.HasValue)).ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(h => h.Id == id && h.IsActive && h.HostId == userId);
            }
            else
            {
                // Khách chỉ lấy homestay đã duyệt
                homestay = await _context.Homestays
                    .Include(h => h.Host)
                    .Include(h => h.Images)
                    .Include(h => h.HomestayAmenities).ThenInclude(ha => ha.Amenity)
                    .Include(h => h.Bookings.Where(b => b.ReviewRating.HasValue)).ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(h => h.Id == id && h.IsActive && h.IsApproved);
            }

            if (homestay == null)
                return null;

            // Tăng view count
            homestay.ViewCount++;
            await _context.SaveChangesAsync();

            var canReview = false;
            if (!string.IsNullOrEmpty(userId))
            {
                canReview = await _context.Bookings
                    .AnyAsync(b => b.UserId == userId &&
                                 b.HomestayId == id &&
                                 b.Status == BookingStatus.Completed &&
                                 b.ReviewRating == null);
            }

            return new HomestayDetailViewModel
            {
                Homestay = homestay,
                Images = homestay.Images.OrderBy(i => i.Order).ToList(),
                Amenities = homestay.HomestayAmenities.Select(ha => ha.Amenity),
                ReviewBookings = homestay.Bookings.Where(b => b.ReviewRating.HasValue).OrderByDescending(b => b.ReviewCreatedAt),
                AverageRating = homestay.AverageRating,
                ReviewCount = homestay.ReviewCount,
                HostName = (homestay.Host != null) ? $"{homestay.Host.FirstName} {homestay.Host.LastName}" : "N/A", 
                HostEmail = homestay.Host?.Email ?? "N/A",
                HostAvatar = homestay.Host?.ProfilePicture ?? string.Empty, // use null-conditional to avoid null reference
                CanReview = canReview
            };
        }

        public async Task<Homestay?> GetHomestayByIdAsync(int id)
        {
            return await _context.Homestays
                .Include(h => h.Images)
                .Include(h => h.HomestayAmenities)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<int> CreateHomestayAsync(CreateHomestayViewModel model, string hostId)
        {
            // Sử dụng trực tiếp các giá trị text từ model (từ external API)
            string wardName = model.Ward ?? string.Empty;
            string districtName = model.District ?? string.Empty;
            string cityName = model.City ?? string.Empty;
            string countryName = model.Country ?? "Vietnam";
            string stateName = model.State ?? "Unknown";
            
            // Nếu state không được cung cấp, tự động xác định dựa trên tỉnh/thành phố
            if (string.IsNullOrEmpty(stateName) || stateName == "Unknown")
            {
                var northernProvinces = new[] { "Hà Nội", "Hải Phòng", "Bắc Ninh", "Hà Nam", "Nam Định", "Thái Bình", "Quảng Ninh", "Bắc Giang", "Hải Dương", "Hưng Yên", "Vĩnh Phúc", "Hòa Bình", "Sơn La", "Điện Biên", "Lai Châu", "Lào Cai", "Yên Bái", "Phú Thọ", "Tuyên Quang", "Hà Giang", "Cao Bằng", "Bắc Kạn", "Lạng Sơn", "Thái Nguyên" };
                var centralProvinces = new[] { "Đà Nẵng", "Huế", "Quảng Nam", "Quảng Ngãi", "Bình Định", "Phú Yên", "Khánh Hòa", "Ninh Thuận", "Bình Thuận", "Kon Tum", "Gia Lai", "Đắk Lắk", "Đắk Nông", "Lâm Đồng", "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Quảng Trị" };
                var southernProvinces = new[] { "Hồ Chí Minh", "Bình Dương", "Đồng Nai", "Bà Rịa - Vũng Tàu", "Cần Thơ", "An Giang", "Bạc Liêu", "Bến Tre", "Cà Mau", "Đồng Tháp", "Hậu Giang", "Kiên Giang", "Long An", "Sóc Trăng", "Tây Ninh", "Tiền Giang", "Trà Vinh", "Vĩnh Long" };
                
                if (northernProvinces.Contains(cityName))
                    stateName = "Miền Bắc";
                else if (centralProvinces.Contains(cityName))
                    stateName = "Miền Trung";
                else if (southernProvinces.Contains(cityName))
                    stateName = "Miền Nam";
                else
                    stateName = "Khác";
            }
            
            // Nếu chưa có tọa độ, tự động geocoding
            decimal latitude = model.Latitude;
            decimal longitude = model.Longitude;
            
            if (latitude == 0 && longitude == 0)
            {
                try
                {
                    _logger.LogInformation("Performing geocoding for address: {Address}", model.Address);
                    var (geocodedLat, geocodedLng) = await _geocodingService.GetCoordinatesAsync($"{model.Address}, {wardName}, {districtName}, {cityName}, {countryName}");
                    
                    if (geocodedLat.HasValue && geocodedLng.HasValue)
                    {
                        latitude = (decimal)geocodedLat.Value;
                        longitude = (decimal)geocodedLng.Value;
                        _logger.LogInformation("Geocoding successful: {Lat}, {Lng}", latitude, longitude);
                    }
                    else
                    {
                        _logger.LogWarning("Geocoding failed for address: {Address}", model.Address);
                        // Sử dụng tọa độ mặc định của Hà Nội
                        latitude = 21.0285m;
                        longitude = 105.8542m;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during geocoding for address: {Address}", model.Address);
                    // Sử dụng tọa độ mặc định của Hà Nội
                    latitude = 21.0285m;
                    longitude = 105.8542m;
                }
            }
              var homestay = new Homestay
            {
                Name = model.Name,
                Description = model.Description,
                Address = model.Address,
                City = cityName,
                State = stateName,
                District = districtName,
                Ward = wardName,
                Country = countryName,
                ZipCode = model.ZipCode,
                Latitude = latitude,
                Longitude = longitude,
                PricePerNight = model.PricePerNight,
                MaxGuests = model.MaxGuests,
                Bedrooms = model.Bedrooms,
                Bathrooms = model.Bathrooms,
                YouTubeVideoId = model.YouTubeVideoId,
                HostId = hostId,
                IsActive = true,
                IsApproved = false // Requires admin approval
            };

            _context.Homestays.Add(homestay);
            await _context.SaveChangesAsync();

            // Handle image uploads
            if (model.Images != null && model.Images.Any())
            {
                var imageUrls = await _fileUploadService.UploadImagesAsync(model.Images, "homestays");
                
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    var homestayImage = new HomestayImage
                    {
                        HomestayId = homestay.Id,
                        ImageUrl = imageUrls[i],
                        IsPrimary = i == 0, // First image is primary
                        Order = i + 1
                    };
                    
                    _context.HomestayImages.Add(homestayImage);
                }
                
                await _context.SaveChangesAsync();
            }

            // Add amenities
            if (model.AmenityIds != null && model.AmenityIds.Any())
            {
                var homestayAmenities = model.AmenityIds.Select(amenityId => new HomestayAmenity
                {
                    HomestayId = homestay.Id,
                    AmenityId = amenityId
                });

                _context.HomestayAmenities.AddRange(homestayAmenities);
                await _context.SaveChangesAsync();
            }

            return homestay.Id;
        }

        public async Task<bool> UpdateHomestayAsync(EditHomestayViewModel model, string hostId)
        {
            var homestay = await _context.Homestays
                .Include(h => h.HomestayAmenities)
                .Include(h => h.Images)
                .FirstOrDefaultAsync(h => h.Id == model.Id && h.HostId == hostId);

            if (homestay == null)
                return false;

            // Sử dụng trực tiếp các giá trị text từ model (từ external API)
            string wardName = model.Ward ?? homestay.Ward ?? string.Empty;
            string districtName = model.District ?? homestay.District ?? string.Empty;
            string cityName = model.City ?? homestay.City ?? string.Empty;
            string countryName = model.Country ?? homestay.Country ?? "Vietnam";
            string stateName = model.State ?? homestay.State ?? string.Empty;
            
            // Nếu state không được cung cấp, tự động xác định dựa trên tỉnh/thành phố
            if (string.IsNullOrEmpty(stateName) || stateName == "Unknown")
            {
                var northernProvinces = new[] { "Hà Nội", "Hải Phòng", "Bắc Ninh", "Hà Nam", "Nam Định", "Thái Bình", "Quảng Ninh", "Bắc Giang", "Hải Dương", "Hưng Yên", "Vĩnh Phúc", "Hòa Bình", "Sơn La", "Điện Biên", "Lai Châu", "Lào Cai", "Yên Bái", "Phú Thọ", "Tuyên Quang", "Hà Giang", "Cao Bằng", "Bắc Kạn", "Lạng Sơn", "Thái Nguyên" };
                var centralProvinces = new[] { "Đà Nẵng", "Huế", "Quảng Nam", "Quảng Ngãi", "Bình Định", "Phú Yên", "Khánh Hòa", "Ninh Thuận", "Bình Thuận", "Kon Tum", "Gia Lai", "Đắk Lắk", "Đắk Nông", "Lâm Đồng", "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Quảng Trị" };
                var southernProvinces = new[] { "Hồ Chí Minh", "Bình Dương", "Đồng Nai", "Bà Rịa - Vũng Tàu", "Cần Thơ", "An Giang", "Bạc Liêu", "Bến Tre", "Cà Mau", "Đồng Tháp", "Hậu Giang", "Kiên Giang", "Long An", "Sóc Trăng", "Tây Ninh", "Tiền Giang", "Trà Vinh", "Vĩnh Long" };
                
                if (northernProvinces.Contains(cityName))
                    stateName = "Miền Bắc";
                else if (centralProvinces.Contains(cityName))
                    stateName = "Miền Trung";
                else if (southernProvinces.Contains(cityName))
                    stateName = "Miền Nam";
                else
                    stateName = "Khác";
            }            homestay.Name = model.Name;
            homestay.Description = model.Description;
            homestay.Address = model.Address;
            homestay.City = cityName;
            homestay.State = stateName;
            homestay.District = districtName;
            homestay.Ward = wardName;
            homestay.Country = countryName;
            homestay.ZipCode = model.ZipCode;
            homestay.YouTubeVideoId = model.YouTubeVideoId;
            homestay.PricePerNight = model.PricePerNight;
            homestay.MaxGuests = model.MaxGuests;
            homestay.Bedrooms = model.Bedrooms;
            homestay.Bathrooms = model.Bathrooms;
            homestay.UpdatedAt = DateTime.UtcNow;

            // Handle image deletions
            if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
            {
                var imagesToDelete = homestay.Images.Where(img => model.ImagesToDelete.Contains(img.Id)).ToList();
                
                foreach (var image in imagesToDelete)
                {
                    // Delete physical file
                    await _fileUploadService.DeleteImageAsync(image.ImageUrl);
                    
                    // Remove from database
                    _context.HomestayImages.Remove(image);
                }
            }

            // Handle new image uploads
            if (model.Images != null && model.Images.Any())
            {
                var imageUrls = await _fileUploadService.UploadImagesAsync(model.Images, "homestays");
                
                var existingImageCount = homestay.Images.Count(img => model.ImagesToDelete == null || !model.ImagesToDelete.Contains(img.Id));
                
                for (int i = 0; i < imageUrls.Count; i++)
                {
                    var homestayImage = new HomestayImage
                    {
                        HomestayId = homestay.Id,
                        ImageUrl = imageUrls[i],
                        IsPrimary = existingImageCount == 0 && i == 0, // First image is primary if no existing images
                        Order = existingImageCount + i + 1
                    };
                    
                    _context.HomestayImages.Add(homestayImage);
                }
            }

            // Update amenities
            _context.HomestayAmenities.RemoveRange(homestay.HomestayAmenities);

            if (model.AmenityIds != null && model.AmenityIds.Any())
            {
                var homestayAmenities = model.AmenityIds.Select(amenityId => new HomestayAmenity
                {
                    HomestayId = homestay.Id,
                    AmenityId = amenityId
                });

                _context.HomestayAmenities.AddRange(homestayAmenities);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteHomestayAsync(int id, string hostId)
        {
            var homestay = await _context.Homestays
                .FirstOrDefaultAsync(h => h.Id == id && h.HostId == hostId);

            if (homestay == null)
                return false;

            homestay.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<HomestayCardViewModel>> GetPopularHomestaysAsync(int count = 8)
        {
            var now = DateTime.Now; // Define 'now' at the beginning of the method

            var homestays = await _context.Homestays
                .Include(h => h.Images)
                .Include(h => h.Bookings.Where(b => b.ReviewRating.HasValue))
                .Where(h => h.IsActive && h.IsApproved)
                .ToListAsync(); // Fetch data from database first

            // Perform client-side ordering and taking
            var popularHomestays = homestays
                .OrderByDescending(h => h.ReviewCount)
                .ThenByDescending(h => h.AverageRating)
                .Take(count)
                .ToList();

            var homestayViewModels = new List<HomestayCardViewModel>();
            
            foreach (var h in popularHomestays)
            {
                // Lấy thông tin về booking hiện tại và sắp tới của homestay
                var currentAndUpcomingBookings = h.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Paid) &&
                           b.CheckOutDate > now)
                    .OrderBy(b => b.CheckInDate)
                    .ToList();
                
                var bookingPeriods = currentAndUpcomingBookings
                    .Select(b => new BookingPeriod { 
                        CheckInDate = b.CheckInDate, 
                        CheckOutDate = b.CheckOutDate 
                    })
                    .ToList();
                
                // Xác định homestay có đang được đặt không
                var isCurrentlyBooked = currentAndUpcomingBookings.Any(b => 
                    b.CheckInDate <= now && b.CheckOutDate > now);
                
                // Tìm ngày tiếp theo khả dụng
                DateTime? nextAvailableDate = null;
                if (isCurrentlyBooked && currentAndUpcomingBookings.Any())
                {
                    var currentBooking = currentAndUpcomingBookings
                        .FirstOrDefault(b => b.CheckInDate <= now && b.CheckOutDate > now);
                    
                    if (currentBooking != null)
                    {
                        nextAvailableDate = currentBooking.CheckOutDate;
                    }
                }
                  homestayViewModels.Add(new HomestayCardViewModel
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    State = h.State,
                    Description = h.Description.Length > 100 ? h.Description.Substring(0, 97) + "..." : h.Description,
                    PricePerNight = h.PricePerNight,
                    PrimaryImage = GetImageUrlWithFallback(h.Images),
                    AverageRating = h.AverageRating,
                    ReviewCount = h.ReviewCount,
                    MaxGuests = h.MaxGuests,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    IsActive = h.IsActive,
                    IsApproved = h.IsApproved,
                    YouTubeVideoId = h.YouTubeVideoId,
                    // Thông tin tình trạng đặt phòng
                    IsBooked = isCurrentlyBooked,
                    NextAvailableDate = nextAvailableDate,
                    BookingPeriods = bookingPeriods
                });
            }
            
            return homestayViewModels;
        }

        public async Task<IEnumerable<HomestayCardViewModel>> GetHostHomestaysAsync(string hostId)
        {
            var now = DateTime.Now;
            var homestays = await _context.Homestays
                .Include(h => h.Images)
                .Include(h => h.Bookings)
                .Where(h => h.HostId == hostId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            var result = new List<HomestayCardViewModel>();
            
            foreach (var h in homestays)
            {
                // Get current and upcoming bookings for this homestay
                var currentAndUpcomingBookings = h.Bookings
                    .Where(b => (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Paid) &&
                            b.CheckOutDate > now)
                    .OrderBy(b => b.CheckInDate)
                    .ToList();
                
                // Check if homestay is currently booked
                var isBooked = currentAndUpcomingBookings.Any(b => b.CheckInDate <= now && b.CheckOutDate > now);
                
                // Find next available date (after current booking ends)
                DateTime? nextAvailable = null;
                if (isBooked)
                {
                    var currentBooking = currentAndUpcomingBookings.FirstOrDefault(b => b.CheckInDate <= now && b.CheckOutDate > now);
                    nextAvailable = currentBooking?.CheckOutDate;
                }
                
                // Create booking periods list
                var bookingPeriods = currentAndUpcomingBookings.Select(b => new BookingPeriod
                {
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate
                }).ToList();
                // Tổng số đặt phòng cho homestay này
                var totalBookings = h.Bookings.Count();
                result.Add(new HomestayCardViewModel
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    State = h.State,
                    Description = h.Description.Length > 100 ? h.Description.Substring(0, 97) + "..." : h.Description,
                    PricePerNight = h.PricePerNight,
                    PrimaryImage = GetImageUrlWithFallback(h.Images),
                    AverageRating = h.AverageRating,
                    ReviewCount = h.ReviewCount,
                    MaxGuests = h.MaxGuests,
                    Bedrooms = h.Bedrooms,
                    Bathrooms = h.Bathrooms,
                    IsActive = h.IsActive,
                    IsApproved = h.IsApproved,
                    YouTubeVideoId = h.YouTubeVideoId,
                    IsBooked = isBooked,
                    NextAvailableDate = nextAvailable,
                    BookingPeriods = bookingPeriods,
                    TotalBookings = totalBookings // Gán tổng số đặt phòng
                });
            }
            
            return result;
        }

        public async Task<bool> ApproveHomestayAsync(int id)
        {
            var homestay = await _context.Homestays.FindAsync(id);
            if (homestay == null)
                return false;

            homestay.IsApproved = true;
            homestay.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectHomestayAsync(int id)
        {
            var homestay = await _context.Homestays.FindAsync(id);
            if (homestay == null)
                return false;

            homestay.IsApproved = false;
            homestay.IsActive = false;
            homestay.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address, string city, string state)
        {
            try
            {
                // Simple geocoding implementation using a basic coordinate lookup
                // In production, you would use a geocoding service like Google Maps API
                _logger.LogInformation("Geocoding address: {Address}, {City}, {State}", address, city, state);
                
                // For now, return coordinates based on major cities in Vietnam
                var coordinates = await Task.FromResult(GetApproximateCoordinates(city, state));
                
                if (coordinates.HasValue)
                {
                    _logger.LogInformation("Geocoded coordinates: {Lat}, {Lng}", coordinates.Value.Latitude, coordinates.Value.Longitude);
                    return (coordinates.Value.Latitude, coordinates.Value.Longitude);
                }

                _logger.LogWarning("Could not geocode address: {Address}, {City}, {State}", address, city, state);
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address: {Address}, {City}, {State}", address, city, state);
                return (null, null);
            }
        }

        private (double Latitude, double Longitude)? GetApproximateCoordinates(string city, string state)
        {
            // Basic coordinate lookup for major Vietnamese cities
            var cityCoordinates = new Dictionary<string, (double Lat, double Lng)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Ho Chi Minh City", (10.8231, 106.6297) },
                { "Saigon", (10.8231, 106.6297) },
                { "Hanoi", (21.0285, 105.8542) },
                { "Da Nang", (16.0544, 108.2022) },
                { "Hue", (16.4674, 107.5905) },
                { "Nha Trang", (12.2388, 109.1967) },
                { "Can Tho", (10.0452, 105.7469) },
                { "Vung Tau", (10.4113, 107.1365) },
                { "Da Lat", (11.9404, 108.4583) },
                { "Hoi An", (15.8801, 108.3380) },
                { "Phu Quoc", (10.2899, 103.9840) },
                { "Mui Ne", (10.9333, 108.1000) }
            };

            foreach (var kvp in cityCoordinates)
            {
                if (city.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                    kvp.Key.Contains(city, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        // Replace image URL handling with centralized method
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

