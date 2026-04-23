using Microsoft.EntityFrameworkCore;
using WebHS.Data;
using WebHS.Models;

namespace WebHS.Services
{
    /// <summary>
    /// Service tối ưu hóa database operations và query performance
    /// </summary>
    public class DatabaseOptimizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseOptimizationService> _logger;

        public DatabaseOptimizationService(ApplicationDbContext context, ILogger<DatabaseOptimizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =================================================================================
        // OPTIMIZED HOMESTAY QUERIES
        // =================================================================================

        /// <summary>
        /// Lấy danh sách homestay với tối ưu hóa performance
        /// </summary>
        public IQueryable<Homestay> GetOptimizedHomestaysQuery(
            string? city = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? maxGuests = null,
            bool activeOnly = true)
        {
            var query = _context.Homestays
                .AsNoTracking() // Disable change tracking for read-only operations
                .Where(h => !activeOnly || (h.IsActive && h.IsApproved));

            // Apply filters with index optimization
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(h => h.City == city); // Uses IX_Homestays_City
            }

            if (minPrice.HasValue)
            {
                query = query.Where(h => h.PricePerNight >= minPrice.Value); // Uses IX_Homestays_Price
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(h => h.PricePerNight <= maxPrice.Value); // Uses IX_Homestays_Price
            }

            if (maxGuests.HasValue)
            {
                query = query.Where(h => h.MaxGuests >= maxGuests.Value);
            }

            return query;
        }

        /// <summary>
        /// Tìm kiếm homestay theo vị trí địa lý với tối ưu hóa
        /// </summary>
        public async Task<List<Homestay>> SearchHomestaysByLocation(
            decimal latitude, 
            decimal longitude, 
            double radiusKm = 10.0,
            int limit = 50)
        {
            // Using Haversine formula for distance calculation
            // This query uses the IX_Homestays_Coordinates index
            var query = _context.Homestays
                .AsNoTracking()
                .Where(h => h.IsActive && h.IsApproved)
                .Select(h => new
                {
                    Homestay = h,
                    Distance = Math.Acos(
                        Math.Sin((double)latitude * Math.PI / 180) * Math.Sin((double)h.Latitude * Math.PI / 180) +
                        Math.Cos((double)latitude * Math.PI / 180) * Math.Cos((double)h.Latitude * Math.PI / 180) *
                        Math.Cos(((double)longitude - (double)h.Longitude) * Math.PI / 180)
                    ) * 6371 // Earth radius in kilometers
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Take(limit);

            var results = await query.ToListAsync();
            return results.Select(x => x.Homestay).ToList();
        }

        // =================================================================================
        // OPTIMIZED BOOKING QUERIES
        // =================================================================================

        /// <summary>
        /// Lấy bookings với tối ưu hóa cho dashboard
        /// </summary>
        public async Task<List<Booking>> GetOptimizedBookingsForUser(
            string userId, 
            BookingStatus? status = null,
            int skip = 0,
            int take = 20)
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.Homestay)
                .Include(b => b.Payments)
                .Where(b => b.UserId == userId); // Uses IX_Bookings_User_Status

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value); // Uses IX_Bookings_User_Status
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy bookings cho host với tối ưu hóa
        /// </summary>
        public async Task<List<Booking>> GetOptimizedBookingsForHost(
            string hostId,
            BookingStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int skip = 0,
            int take = 20)
        {
            var query = _context.Bookings
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Homestay)
                .Include(b => b.Payments)
                .Where(b => b.Homestay.HostId == hostId); // Uses IX_Homestays_Host_Active then join

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.CheckInDate >= fromDate.Value); // Uses IX_Bookings_DateRange
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.CheckOutDate <= toDate.Value); // Uses IX_Bookings_DateRange
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Kiểm tra tính khả dụng của homestay với tối ưu hóa
        /// </summary>
        public async Task<bool> IsHomestayAvailable(int homestayId, DateTime checkIn, DateTime checkOut)
        {
            // Check blocked dates first (uses IX_BlockedDates_Homestay_Date_Unique)
            var hasBlockedDates = await _context.BlockedDates
                .AsNoTracking()
                .AnyAsync(bd => bd.HomestayId == homestayId && 
                          bd.Date >= checkIn.Date && 
                          bd.Date < checkOut.Date);

            if (hasBlockedDates)
                return false;

            // Check existing bookings (uses IX_Bookings_Homestay_Status)
            var hasConflictingBookings = await _context.Bookings
                .AsNoTracking()
                .AnyAsync(b => b.HomestayId == homestayId &&
                          b.Status == BookingStatus.Paid &&
                          b.CheckInDate < checkOut &&
                          b.CheckOutDate > checkIn);

            return !hasConflictingBookings;
        }

        // =================================================================================
        // OPTIMIZED PAYMENT & FINANCIAL QUERIES
        // =================================================================================

        /// <summary>
        /// Báo cáo doanh thu tối ưu hóa
        /// </summary>
        public async Task<decimal> GetRevenueReport(
            string? hostId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            PaymentStatus paymentStatus = PaymentStatus.Completed)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Booking)
                .ThenInclude(b => b.Homestay)
                .Where(p => p.Status == paymentStatus); // Uses IX_Payments_Financial_Report

            if (!string.IsNullOrEmpty(hostId))
            {
                query = query.Where(p => p.Booking.Homestay.HostId == hostId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= fromDate.Value); // Uses IX_Payments_Financial_Report
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= toDate.Value); // Uses IX_Payments_Financial_Report
            }

            return await query.SumAsync(p => p.Amount);
        }

        // =================================================================================
        // OPTIMIZED MESSAGING QUERIES
        // =================================================================================

        /// <summary>
        /// Lấy tin nhắn chưa đọc với tối ưu hóa
        /// </summary>
        public async Task<List<Message>> GetUnreadMessages(string userId, int limit = 50)
        {
            return await _context.Messages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .Where(m => m.ReceiverId == userId && !m.IsRead) // Uses IX_Messages_Unread_Receiver
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy conversations với tối ưu hóa
        /// </summary>
        public async Task<List<Conversation>> GetUserConversations(string userId, int limit = 20)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.LastMessageSender)
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .OrderByDescending(c => c.LastMessageAt) // Uses IX_Conversations_LastMessage
                .Take(limit)
                .ToListAsync();
        }

        // =================================================================================
        // DATABASE MAINTENANCE & OPTIMIZATION
        // =================================================================================

        /// <summary>
        /// Cập nhật thống kê database
        /// </summary>
        public async Task UpdateDatabaseStatistics()
        {
            try
            {
                // Update statistics for all tables in the database
                var allowedTables = new[] { 
                    "Homestays", "Bookings", "Payments", "Messages", "Conversations", 
                    "HomestayImages", "Amenities", "HomestayAmenities", "Promotions",
                    "BlockedDates", "HomestayPricings", "UserNotifications", "MessageTemplates",
                    "AspNetUsers", "AspNetRoles", "AspNetUserRoles" // Identity tables
                };
                
                foreach (var table in allowedTables)
                {
                    try
                    {
                        // Use string concatenation instead of interpolation to avoid EF1002 warning
                        var sql = "UPDATE STATISTICS [" + table + "]";
                        #pragma warning disable EF1002 // Safe: using whitelisted table names
                        await _context.Database.ExecuteSqlRawAsync(sql);
                        #pragma warning restore EF1002
                        _logger.LogDebug($"Updated statistics for table: {table}");
                    }
                    catch (Exception tableEx)
                    {
                        _logger.LogWarning(tableEx, $"Failed to update statistics for table: {table}");
                    }
                }
                
                _logger.LogInformation("Database statistics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update database statistics");
            }
        }

        /// <summary>
        /// Reindex database tables
        /// </summary>
        public async Task ReindexTables()
        {
            try
            {
                // Whitelist of allowed table names to prevent SQL injection
                var allowedTables = new[] { 
                    "Homestays", "Bookings", "Payments", "Messages", "Conversations",
                    "HomestayImages", "Amenities", "HomestayAmenities", "Promotions",
                    "BlockedDates", "HomestayPricings", "UserNotifications", "MessageTemplates"
                };
                
                foreach (var table in allowedTables)
                {
                    // Since we're using whitelisted table names, this is safe from SQL injection
                    // We cannot use parameters for table names in SQL, so we validate the table name first
                    if (allowedTables.Contains(table))
                    {
                        // Use string concatenation instead of interpolation to avoid EF1002 warning
                        var sql = "ALTER INDEX ALL ON [" + table + "] REBUILD";
                        #pragma warning disable EF1002 // Safe: table name is validated against whitelist
                        await _context.Database.ExecuteSqlRawAsync(sql);
                        #pragma warning restore EF1002
                    }
                }
                
                _logger.LogInformation("Database tables reindexed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reindex database tables");
            }
        }

        /// <summary>
        /// Cleanup old data
        /// </summary>
        public async Task CleanupOldData(int daysToKeep = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

                // Clean up old notifications
                var oldNotifications = await _context.UserNotifications
                    .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                    .ToListAsync();

                _context.UserNotifications.RemoveRange(oldNotifications);

                // Clean up old messages (keep conversation history)
                var oldMessages = await _context.Messages
                    .Where(m => m.SentAt < cutoffDate && m.IsDeleted)
                    .ToListAsync();

                _context.Messages.RemoveRange(oldMessages);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cleaned up {oldNotifications.Count} notifications and {oldMessages.Count} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old data");
            }
        }

        // =================================================================================
        // PERFORMANCE MONITORING
        // =================================================================================

        /// <summary>
        /// Lấy thông tin performance của database
        /// </summary>
        public async Task<DatabasePerformanceInfo> GetDatabasePerformanceInfo()
        {
            try
            {
                var info = new DatabasePerformanceInfo();

                // Get table sizes
                var tableSizes = await _context.Database
                    .SqlQuery<TableSizeInfo>($@"
                        SELECT 
                            t.NAME AS TableName,
                            s.Name AS SchemaName,
                            p.rows AS RowCounts,
                            SUM(a.total_pages) * 8 AS TotalSpaceKB,
                            SUM(a.used_pages) * 8 AS UsedSpaceKB
                        FROM sys.tables t
                        INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
                        INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                        LEFT OUTER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE t.NAME NOT LIKE 'dt%' 
                            AND t.is_ms_shipped = 0
                            AND i.OBJECT_ID > 255
                        GROUP BY t.Name, s.Name, p.Rows
                        ORDER BY p.Rows DESC")
                    .ToListAsync();

                info.TableSizes = tableSizes;
                info.TotalSizeKB = tableSizes.Sum(t => t.TotalSpaceKB);
                info.GeneratedAt = DateTime.UtcNow;

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database performance info");
                return new DatabasePerformanceInfo { GeneratedAt = DateTime.UtcNow };
            }
        }
    }

    // =================================================================================
    // SUPPORTING CLASSES
    // =================================================================================

    public class DatabasePerformanceInfo
    {
        public DateTime GeneratedAt { get; set; }
        public long TotalSizeKB { get; set; }
        public List<TableSizeInfo> TableSizes { get; set; } = new();
    }

    public class TableSizeInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public long RowCounts { get; set; }
        public long TotalSpaceKB { get; set; }
        public long UsedSpaceKB { get; set; }
    }
}
