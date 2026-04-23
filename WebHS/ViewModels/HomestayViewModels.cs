using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebHS.ViewModels
{
    public class HomestaySearchViewModel
    {
        public string? Location { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int Guests { get; set; } = 1;
        public int NumberOfGuests { get; set; } = 1;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? HomestayType { get; set; }
        public int[]? AmenityIds { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public string SortBy { get; set; } = "popular"; // popular, price_low, price_high, rating
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class HomestayListViewModel
    {
        public IEnumerable<HomestayCardViewModel> Homestays { get; set; } = new List<HomestayCardViewModel>();
        public HomestaySearchViewModel Search { get; set; } = new HomestaySearchViewModel();
        public HomestaySearchViewModel SearchModel { get; set; } = new HomestaySearchViewModel();
        public IEnumerable<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage => Search.Page;
        public bool HasPreviousPage => Search.Page > 1;
        public bool HasNextPage => Search.Page < TotalPages;
        
        // Route data method for pagination
        public Dictionary<string, string> GetRouteData(int page)
        {
            return new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "location", SearchModel.Location ?? "" },
                { "checkInDate", SearchModel.CheckInDate?.ToString("yyyy-MM-dd") ?? "" },
                { "checkOutDate", SearchModel.CheckOutDate?.ToString("yyyy-MM-dd") ?? "" },
                { "guests", SearchModel.Guests.ToString() },
                { "minPrice", SearchModel.MinPrice?.ToString() ?? "" },
                { "maxPrice", SearchModel.MaxPrice?.ToString() ?? "" },
                { "homestayType", SearchModel.HomestayType ?? "" },
                { "sortBy", SearchModel.SortBy ?? "popular" }
            };
        }
    }

    public class HomestayCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HomestayType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public string PrimaryImage { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int MaxGuests { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int NumberOfBedrooms { get; set; }
        public int NumberOfBathrooms { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public string? YouTubeVideoId { get; set; }
        
        // Thông tin tình trạng đặt phòng
        public bool IsBooked { get; set; } = false;
        public DateTime? NextAvailableDate { get; set; }
        public List<BookingPeriod> BookingPeriods { get; set; } = new List<BookingPeriod>();
        public int TotalBookings { get; set; } // Tổng số đặt phòng cho homestay
    }
    
    public class BookingPeriod
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
    }
    

    public class HomestayDetailViewModel
    {
        public Homestay Homestay { get; set; } = new Homestay();
        public List<HomestayImage> Images { get; set; } = new List<HomestayImage>();
        public IEnumerable<Amenity> Amenities { get; set; } = new List<Amenity>();
        public IEnumerable<Booking> ReviewBookings { get; set; } = new List<Booking>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string HostName { get; set; } = string.Empty;
        public string HostAvatar { get; set; } = string.Empty;
        public bool CanReview { get; set; }
        public string HostEmail { get; internal set; } = string.Empty;
    }
}

