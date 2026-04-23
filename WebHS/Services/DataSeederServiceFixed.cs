using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    public class DataSeederServiceFixed
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeederServiceFixed> _logger;

        public DataSeederServiceFixed(ApplicationDbContext context, ILogger<DataSeederServiceFixed> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");

                await SeedAmenitiesAsync();
                await SeedHomestaysAsync();
                await SeedPromotionsAsync();
                await SeedBookingsAsync();
                await SeedReviewsAsync();
                await SeedPaymentsAsync();

                _logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding data");
                throw;
            }
        }

        private async Task SeedAmenitiesAsync()
        {
            try
            {
                if (await _context.Amenities.AnyAsync())
                {
                    _logger.LogInformation("Amenities already exist, skipping seed");
                    return;
                }

                var amenities = new List<Amenity>
                {
                    new Amenity { Name = "WiFi miễn phí", Description = "Internet tốc độ cao", Icon = "fas fa-wifi", IsActive = true },
                    new Amenity { Name = "Điều hòa", Description = "Hệ thống điều hòa nhiệt độ", Icon = "fas fa-snowflake", IsActive = true },
                    new Amenity { Name = "Tivi", Description = "TV màn hình phẳng", Icon = "fas fa-tv", IsActive = true },
                    new Amenity { Name = "Tủ lạnh", Description = "Tủ lạnh mini", Icon = "fas fa-cube", IsActive = true },
                    new Amenity { Name = "Bếp", Description = "Bếp nấu ăn đầy đủ", Icon = "fas fa-utensils", IsActive = true },
                    new Amenity { Name = "Máy giặt", Description = "Máy giặt tự động", Icon = "fas fa-tshirt", IsActive = true },
                    new Amenity { Name = "Chỗ đậu xe", Description = "Chỗ đậu xe miễn phí", Icon = "fas fa-car", IsActive = true },
                    new Amenity { Name = "Hồ bơi", Description = "Hồ bơi riêng hoặc chung", Icon = "fas fa-swimmer", IsActive = true },
                    new Amenity { Name = "Gym", Description = "Phòng tập thể dục", Icon = "fas fa-dumbbell", IsActive = true },
                    new Amenity { Name = "Ban công", Description = "Ban công với view đẹp", Icon = "fas fa-building", IsActive = true },
                    new Amenity { Name = "Vườn", Description = "Khu vườn riêng", Icon = "fas fa-leaf", IsActive = true },
                    new Amenity { Name = "BBQ", Description = "Khu vực nướng BBQ", Icon = "fas fa-fire", IsActive = true },
                    new Amenity { Name = "Thang máy", Description = "Thang máy trong tòa nhà", Icon = "fas fa-elevator", IsActive = true },
                    new Amenity { Name = "An ninh 24/7", Description = "Bảo vệ 24/7", Icon = "fas fa-shield-alt", IsActive = true },
                    new Amenity { Name = "Phòng khách riêng", Description = "Khu vực sinh hoạt chung", Icon = "fas fa-couch", IsActive = true }
                };

                _context.Amenities.AddRange(amenities);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {amenities.Count} amenities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding amenities");
            }
        }

        private async Task SeedHomestaysAsync()
        {
            try
            {
                if (await _context.Homestays.AnyAsync())
                {
                    _logger.LogInformation("Homestays already exist, skipping seed");
                    return;
                }                // Get host users (dựa vào email pattern thay vì IsHost property)
                var hosts = await _context.Users
                    .Where(u => u.Email != null && u.Email.StartsWith("host"))
                    .ToListAsync();
                    
                if (!hosts.Any())
                {
                    _logger.LogWarning("No host users found, skipping homestay seeding");
                    return;
                }

                var random = new Random();
                
                var homestays = new List<Homestay>();
                
                // Homestay 1
                homestays.Add(new Homestay
                {
                    Name = "Villa Luxury Đà Lạt",
                    Description = "Villa cao cấp với view núi tuyệt đẹp, không gian rộng rãi, thoáng mát. Phù hợp cho gia đình hoặc nhóm bạn nghỉ dưỡng. Có vườn hoa, hồ bơi riêng và khu BBQ ngoài trời.",
                    Address = "123 Đường Trần Phú",
                    City = "Đà Lạt",
                    State = "Lâm Đồng",
                    ZipCode = "670000",
                    Latitude = 11.9404m,
                    Longitude = 108.4583m,
                    PricePerNight = 2500000m,
                    MaxGuests = 8,
                    Bedrooms = 4,
                    Bathrooms = 3,
                    Rules = "Không hút thuốc trong nhà. Không tổ chức tiệc ồn ào sau 22h. Giữ gìn vệ sinh chung.",
                    HostId = hosts[0].Id,  // Assign to first host
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(60, 120))
                });
                
                // Homestay 2
                if (hosts.Count >= 2)
                {
                    homestays.Add(new Homestay
                    {
                        Name = "Căn hộ hiện đại Sài Gòn",
                        Description = "Căn hộ 2 phòng ngủ hiện đại, view thành phố tuyệt đẹp, đầy đủ tiện nghi. Gần trung tâm mua sắm Vincom, bến Nhà Rồng và các điểm tham quan nổi tiếng.",
                        Address = "456 Nguyễn Huệ",
                        City = "Thành phố Hồ Chí Minh",
                        State = "Hồ Chí Minh",
                        ZipCode = "700000",
                        Latitude = 10.7769m,
                        Longitude = 106.7009m,
                        PricePerNight = 1800000m,
                        MaxGuests = 4,
                        Bedrooms = 2,
                        Bathrooms = 2,
                        Rules = "Check-in từ 14h, check-out trước 12h. Không mang thú cưng. Báo trước khi có khách đến thăm.",
                        HostId = hosts[1].Id,  // Assign to second host
                        IsActive = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(50, 100))
                    });
                }
                
                // Homestay 3
                if (hosts.Count >= 3)
                {
                    homestays.Add(new Homestay
                    {
                        Name = "Biệt thự biển Nha Trang",
                        Description = "Biệt thự mặt biển với hồ bơi riêng, view biển 180 độ tuyệt đẹp. Phù hợp cho kỳ nghỉ lãng mạn hoặc gia đình. Cách bãi biển chỉ 50m, có khu vực BBQ và sân vườn.",
                        Address = "789 Trần Phú",
                        City = "Nha Trang",
                        State = "Khánh Hòa",
                        ZipCode = "650000",
                        Latitude = 12.2388m,
                        Longitude = 109.1967m,
                        PricePerNight = 3200000m,
                        MaxGuests = 6,
                        Bedrooms = 3,
                        Bathrooms = 2,
                        Rules = "Bảo vệ môi trường biển. Không vứt rác xuống biển. Trẻ em phải có người lớn giám sát khi ra biển.",
                        HostId = hosts[Math.Min(2, hosts.Count - 1)].Id,
                        IsActive = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(40, 90))
                    });
                }
                
                // Homestay 4
                if (hosts.Count >= 4)
                {
                    homestays.Add(new Homestay
                    {
                        Name = "Nhà phố cổ Hà Nội",
                        Description = "Nhà phố truyền thống trong khu phố cổ, gần Hồ Hoàn Kiếm, chợ Đồng Xuân và các điểm du lịch nổi tiếng. Thiết kế kết hợp hiện đại và cổ điển, không gian ấm cúng.",
                        Address = "101 Hàng Bạc",
                        City = "Hà Nội",
                        State = "Hà Nội",
                        ZipCode = "100000",
                        Latitude = 21.0285m,
                        Longitude = 105.8542m,
                        PricePerNight = 1500000m,
                        MaxGuests = 6,
                        Bedrooms = 3,
                        Bathrooms = 2,
                        Rules = "Giữ yên lặng sau 23h do khu vực dân cư đông đúc. Cất giữ tài sản cẩn thận.",
                        HostId = hosts[Math.Min(3, hosts.Count - 1)].Id,
                        IsActive = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30, 80))
                    });
                }

                // Homestay 5
                homestays.Add(new Homestay
                {
                    Name = "Resort mini Phú Quốc",
                    Description = "Khu nghỉ dưỡng nhỏ trên đảo Phú Quốc, cách bãi biển Bãi Trường 100m, có khu vườn tropical và hồ bơi. View hoàng hôn tuyệt đẹp, không gian riêng tư và yên tĩnh.",
                    Address = "234 Bãi Trường",
                    City = "Phú Quốc",
                    State = "Kiên Giang",
                    ZipCode = "920000",
                    Latitude = 10.2899m,
                    Longitude = 103.9840m,
                    PricePerNight = 2800000m,
                    MaxGuests = 10,
                    Bedrooms = 5,
                    Bathrooms = 4,
                    Rules = "Bảo vệ sinh thái biển. Không câu cá trong khu vực bảo tồn. Check-out muộn nhất 11h.",
                    HostId = hosts[0].Id,  // Assign back to first host
                    IsActive = true,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(20, 70))
                });

                if (homestays.Any())
                {
                    _context.Homestays.AddRange(homestays);
                    await _context.SaveChangesAsync();
                    
                    // Add images for homestays
                    await SeedHomestayImagesAsync();

                    // Add amenities for homestays
                    await SeedHomestayAmenitiesAsync();

                    _logger.LogInformation($"Seeded {homestays.Count} homestays with {hosts.Count} hosts");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding homestays");
            }
        }

        private async Task SeedHomestayImagesAsync()
        {
            try
            {
                var homestays = await _context.Homestays.ToListAsync();
                var images = new List<HomestayImage>();

                foreach (var homestay in homestays)
                {
                    // Add 3-4 sample images for each homestay using existing placeholder
                    for (int i = 1; i <= 3; i++)
                    {
                        images.Add(new HomestayImage
                        {
                            HomestayId = homestay.Id,
                            ImageUrl = "/images/placeholder-homestay.svg", // Use existing placeholder
                            IsPrimary = i == 1,
                            Order = i
                        });
                    }
                }

                _context.HomestayImages.AddRange(images);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {images.Count} homestay images");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding homestay images");
            }
        }

        private async Task SeedHomestayAmenitiesAsync()
        {
            try
            {
                var homestays = await _context.Homestays.ToListAsync();
                var amenities = await _context.Amenities.ToListAsync();
                
                if (!homestays.Any() || !amenities.Any())
                {
                    _logger.LogWarning("No homestays or amenities found for amenity assignment");
                    return;
                }
                
                var homestayAmenities = new List<HomestayAmenity>();
                var random = new Random();

                foreach (var homestay in homestays)
                {
                    // Randomly assign 5-10 amenities to each homestay
                    int amenityCount = Math.Min(random.Next(5, 11), amenities.Count);
                    var randomAmenities = amenities.OrderBy(x => Guid.NewGuid()).Take(amenityCount);
                    
                    foreach (var amenity in randomAmenities)
                    {
                        homestayAmenities.Add(new HomestayAmenity
                        {
                            HomestayId = homestay.Id,
                            AmenityId = amenity.Id
                        });
                    }
                }

                _context.HomestayAmenities.AddRange(homestayAmenities);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {homestayAmenities.Count} homestay amenities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding homestay amenities");
            }
        }

        private async Task SeedPromotionsAsync()
        {
            try
            {
                if (await _context.Promotions.AnyAsync())
                {
                    _logger.LogInformation("Promotions already exist, skipping seed");
                    return;
                }

                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin1@webhs.com");
                
                var promotions = new List<Promotion>
                {
                    new Promotion
                    {
                        Code = "WELCOME10",
                        Name = "Chào mừng khách hàng mới",
                        Description = "Giảm 10% cho khách hàng đặt phòng lần đầu",
                        Type = PromotionType.Percentage,
                        Value = 10m,
                        MinOrderAmount = 500000m,
                        MaxDiscountAmount = 200000m,
                        IsActive = true,
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        EndDate = DateTime.UtcNow.AddDays(60),
                        UsageLimit = 100,
                        UsedCount = 15,
                        CreatedByUserId = adminUser?.Id
                    },
                    new Promotion
                    {
                        Code = "SUMMER2024",
                        Name = "Khuyến mãi mùa hè",
                        Description = "Giảm 15% cho các booking trong mùa hè",
                        Type = PromotionType.Percentage,
                        Value = 15m,
                        MinOrderAmount = 1000000m,
                        MaxDiscountAmount = 500000m,
                        IsActive = true,
                        StartDate = DateTime.UtcNow.AddDays(-15),
                        EndDate = DateTime.UtcNow.AddDays(45),
                        UsageLimit = 200,
                        UsedCount = 45,
                        CreatedByUserId = adminUser?.Id
                    },
                    new Promotion
                    {
                        Code = "WEEKEND50",
                        Name = "Giảm giá cuối tuần",
                        Description = "Giảm 50,000 VNĐ cho booking cuối tuần",
                        Type = PromotionType.FixedAmount,
                        Value = 50000m,
                        MinOrderAmount = 800000m,
                        IsActive = true,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        EndDate = DateTime.UtcNow.AddDays(30),
                        UsageLimit = 50,
                        UsedCount = 12,
                        CreatedByUserId = adminUser?.Id
                    }
                };

                _context.Promotions.AddRange(promotions);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {promotions.Count} promotions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding promotions");
            }
        }

        private async Task SeedBookingsAsync()
        {
            try
            {
                if (await _context.Bookings.AnyAsync())
                {
                    _logger.LogInformation("Bookings already exist, skipping seed");
                    return;
                }                // Get all regular users (non-admin, dựa vào email pattern thay vì IsHost)
                var users = await _context.Users
                    .Where(u => u.Email != null && !u.Email.StartsWith("admin") && !u.Email.StartsWith("host"))
                    .ToListAsync();
                    
                var homestays = await _context.Homestays.ToListAsync();
                var promotions = await _context.Promotions.ToListAsync() ?? new List<Promotion>();

                if (!users.Any() || !homestays.Any())
                {
                    _logger.LogWarning("Required data not found for booking seeding");
                    return;
                }

                var bookings = new List<Booking>();
                var random = new Random();
                var today = DateTime.UtcNow;

                // Helper function to distribute bookings evenly
                string GetRandomUserId() => users.Count > 0 ? users[random.Next(users.Count)].Id : string.Empty;
                Homestay? GetRandomHomestay() => homestays.Count > 0 ? homestays[random.Next(homestays.Count)] : null;

                // 1. Past completed bookings (check-out date in the past)
                for (int i = 0; i < 10; i++)
                {
                    var homestay = GetRandomHomestay();
                    if (homestay == null) continue;
                    
                    try
                    {
                        // Past booking: 7-120 days ago
                        var checkOutDate = today.AddDays(-random.Next(7, 120));
                        var stayDuration = random.Next(1, 8); // 1-7 nights
                        var checkInDate = checkOutDate.AddDays(-stayDuration);
                        var numberOfNights = (checkOutDate - checkInDate).Days;
                        var totalAmount = homestay.PricePerNight * numberOfNights;
                        
                        // Most past bookings should be completed
                        var status = random.Next(1, 101) <= 85 
                            ? BookingStatus.Completed 
                            : (random.Next(1, 101) <= 70 ? BookingStatus.Cancelled : BookingStatus.Cancelled);

                        var userId = GetRandomUserId();
                        if (string.IsNullOrEmpty(userId)) continue;

                        var booking = new Booking
                        {
                            CheckInDate = checkInDate,
                            CheckOutDate = checkOutDate,
                            NumberOfGuests = random.Next(1, Math.Min(homestay.MaxGuests, 10) + 1),
                            TotalAmount = totalAmount,
                            DiscountAmount = 0,
                            FinalAmount = totalAmount,
                            Status = status,
                            Notes = $"Booking trong quá khứ #{i + 1}",
                            CreatedAt = checkInDate.AddDays(-random.Next(1, 14)),
                            UserId = userId,
                            HomestayId = homestay.Id
                        };

                        ApplyRandomPromotion(booking, promotions, totalAmount, random);
                        bookings.Add(booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating past booking #{Index}", i);
                    }
                }

                // 2. Current active bookings (check-in date in the past, check-out date in the future)
                for (int i = 0; i < 3; i++)
                {
                    var homestay = GetRandomHomestay();
                    if (homestay == null) continue;
                    
                    try
                    {
                        // Checked in 1-6 days ago, checking out 1-7 days from now
                        var checkInDate = today.AddDays(-random.Next(1, 7));
                        var checkOutDate = today.AddDays(random.Next(1, 8));
                        var numberOfNights = (checkOutDate - checkInDate).Days;
                        var totalAmount = homestay.PricePerNight * numberOfNights;

                        var userId = GetRandomUserId();
                        if (string.IsNullOrEmpty(userId)) continue;

                        var booking = new Booking
                        {
                            CheckInDate = checkInDate,
                            CheckOutDate = checkOutDate,
                            NumberOfGuests = random.Next(1, Math.Min(homestay.MaxGuests, 10) + 1),
                            TotalAmount = totalAmount,
                            DiscountAmount = 0,
                            FinalAmount = totalAmount,
                            Status = BookingStatus.Paid,
                            Notes = $"Khách đang lưu trú #{i + 1}",
                            CreatedAt = checkInDate.AddDays(-random.Next(1, 21)),
                            UserId = userId,
                            HomestayId = homestay.Id
                        };

                        ApplyRandomPromotion(booking, promotions, totalAmount, random);
                        bookings.Add(booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating current booking #{Index}", i);
                    }
                }

                // 3. Upcoming confirmed bookings (both dates in the future)
                for (int i = 0; i < 5; i++)
                {
                    var homestay = GetRandomHomestay();
                    if (homestay == null) continue;
                    
                    try
                    {
                        // Check in 1-45 days from now
                        var checkInDate = today.AddDays(random.Next(1, 46));
                        var stayDuration = random.Next(1, 10); // 1-9 nights
                        var checkOutDate = checkInDate.AddDays(stayDuration);
                        var numberOfNights = (checkOutDate - checkInDate).Days;
                        var totalAmount = homestay.PricePerNight * numberOfNights;

                        // Most future bookings will be confirmed, some still pending
                        var status = random.Next(1, 101) <= 80 
                            ? BookingStatus.Paid 
                            : BookingStatus.Paid;

                        var userId = GetRandomUserId();
                        if (string.IsNullOrEmpty(userId)) continue;

                        var booking = new Booking
                        {
                            CheckInDate = checkInDate,
                            CheckOutDate = checkOutDate,
                            NumberOfGuests = random.Next(1, Math.Min(homestay.MaxGuests, 10) + 1),
                            TotalAmount = totalAmount,
                            DiscountAmount = 0,
                            FinalAmount = totalAmount,
                            Status = status,
                            Notes = $"Đặt phòng trong tương lai #{i + 1}",
                            CreatedAt = today.AddDays(-random.Next(1, 15)),
                            UserId = userId,
                            HomestayId = homestay.Id
                        };

                        ApplyRandomPromotion(booking, promotions, totalAmount, random);
                        bookings.Add(booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating future booking #{Index}", i);
                    }
                }

                if (bookings.Any())
                {
                    _context.Bookings.AddRange(bookings);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Seeded {bookings.Count} bookings for {users.Count} users");
                }
                else
                {
                    _logger.LogWarning("No bookings were created during seeding");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding bookings");
            }
        }

        // Helper method to apply promotions
        private void ApplyRandomPromotion(Booking booking, List<Promotion> promotions, decimal totalAmount, Random random)
        {
            try
            {
                // Apply promotion randomly (25% chance)
                if (random.Next(1, 101) <= 25 && promotions != null && promotions.Any())
                {
                    var promotion = promotions[random.Next(promotions.Count)];
                    booking.PromotionId = promotion.Id;
                    
                    if (promotion.Type == PromotionType.Percentage)
                    {
                        booking.DiscountAmount = Math.Min(
                            totalAmount * promotion.Value / 100,
                            promotion.MaxDiscountAmount ?? decimal.MaxValue
                        );
                    }
                    else
                    {
                        booking.DiscountAmount = promotion.Value;
                    }
                    
                    booking.FinalAmount = Math.Max(0, totalAmount - booking.DiscountAmount);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing
                booking.PromotionId = null;
                booking.DiscountAmount = 0;
                booking.FinalAmount = totalAmount;
                _logger.LogError(ex, "Error applying promotion to booking");
            }
        }

        private async Task SeedReviewsAsync()
        {
            try
            {
                var completedBookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Completed && b.ReviewRating == null)
                    .ToListAsync();

                if (!completedBookings.Any())
                {
                    return;
                }

                var reviewComments = new[]
                {
                    "Chỗ ở rất tuyệt vời, sạch sẽ và thoải mái. Chủ nhà rất thân thiện và hỗ trợ nhiệt tình.",
                    "Vị trí rất thuận tiện, gần các điểm tham quan. Phòng được trang bị đầy đủ tiện nghi.",
                    "Không gian rộng rãi, view đẹp. Rất phù hợp cho gia đình có trẻ nhỏ.",
                    "Giá cả hợp lý, chất lượng tốt. Sẽ quay lại lần sau khi có dịp.",
                    "Homestay như hình ảnh mô tả, không gian yên tĩnh, thích hợp nghỉ ngơi."
                };

                var random = new Random();

                foreach (var booking in completedBookings.Take(5))
                {
                    try
                    {
                        booking.ReviewRating = random.Next(3, 6); // Rating 3-5 stars
                        booking.ReviewComment = reviewComments[random.Next(reviewComments.Length)];
                        booking.ReviewIsActive = true;
                        booking.ReviewCreatedAt = booking.CheckOutDate.AddDays(random.Next(1, 7));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding review to booking {BookingId}", booking.Id);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded reviews for {completedBookings.Take(5).Count()} completed bookings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding reviews");
            }
        }

        private async Task SeedPaymentsAsync()
        {
            try
            {
                if (await _context.Payments.AnyAsync())
                {
                    _logger.LogInformation("Payments already exist, skipping seed");
                    return;
                }

                var bookings = await _context.Bookings.Include(b => b.User).ToListAsync();
                if (!bookings.Any())
                {
                    _logger.LogInformation("No bookings found for payment seeding");
                    return;
                }

                var payments = new List<Payment>();
                var random = new Random();
                var paymentMethods = Enum.GetValues<PaymentMethod>();

                foreach (var booking in bookings)
                {
                    try
                    {
                        // Determine payment status based on booking status
                        PaymentStatus paymentStatus;
                        DateTime? completedAt = null;
                        string notes = $"Thanh toán cho booking #{booking.Id}";
                        
                        switch (booking.Status)
                        {                            case BookingStatus.Paid:
                                paymentStatus = PaymentStatus.Completed;
                                completedAt = booking.CreatedAt.AddMinutes(random.Next(15, 180));
                                break;
                            
                            case BookingStatus.Completed:
                                paymentStatus = PaymentStatus.Completed;
                                completedAt = booking.CreatedAt.AddMinutes(random.Next(15, 180));
                                break;
                              case BookingStatus.Cancelled:
                                paymentStatus = random.Next(1, 101) <= 70 ? PaymentStatus.Cancelled : PaymentStatus.Refunded;
                                if (paymentStatus == PaymentStatus.Refunded)
                                {
                                    notes = $"Hoàn tiền cho booking #{booking.Id} đã hủy";
                                    completedAt = booking.CheckOutDate.AddDays(random.Next(1, 5));
                                }
                                break;
                            
                            default:
                                paymentStatus = PaymentStatus.Completed;
                                completedAt = booking.CreatedAt.AddMinutes(random.Next(15, 180));
                                break;
                        }

                        // Create payment for this booking
                        var transactionId = $"TXN-{DateTime.Now.ToString("yyyyMMdd")}-{booking.Id}-{Guid.NewGuid().ToString().Substring(0, 8)}";
                        
                        var payment = new Payment
                        {
                            TransactionId = transactionId,
                            Amount = booking.FinalAmount,
                            PaymentMethod = paymentMethods[random.Next(paymentMethods.Length)],
                            Status = paymentStatus,
                            Notes = notes,
                            CreatedAt = booking.CreatedAt.AddMinutes(random.Next(5, 60)),
                            CompletedAt = completedAt,
                            UserId = booking.UserId,
                            BookingId = booking.Id
                        };

                        payments.Add(payment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating payment for booking {BookingId}", booking.Id);
                    }
                }

                if (payments.Any())
                {
                    _context.Payments.AddRange(payments);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Seeded {payments.Count} payments for {bookings.Count} bookings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding payments");
            }
        }
    }
}
