using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebHS.Models;
using WebHS.Services;
using WebHSUser = WebHS.Models.User;
using WebHSPromotion = WebHS.Models.Promotion;

namespace WebHS.Data
{
    public class ApplicationDbContext : IdentityDbContext<WebHSUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Homestay> Homestays { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<HomestayAmenity> HomestayAmenities { get; set; }
        public DbSet<HomestayImage> HomestayImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<WebHSPromotion> Promotions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<BlockedDate> BlockedDates { get; set; }
        public DbSet<HomestayPricing> HomestayPricings { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<MessageTemplate> MessageTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================================================
            // CẤU HÌNH RELATIONSHIPS & FOREIGN KEYS
            // =================================================================================

            // Configure HomestayAmenity many-to-many relationship
            modelBuilder.Entity<HomestayAmenity>()
                .HasKey(ha => new { ha.HomestayId, ha.AmenityId });

            modelBuilder.Entity<HomestayAmenity>()
                .HasOne(ha => ha.Homestay)
                .WithMany(h => h.HomestayAmenities)
                .HasForeignKey(ha => ha.HomestayId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HomestayAmenity>()
                .HasOne(ha => ha.Amenity)
                .WithMany(a => a.HomestayAmenities)
                .HasForeignKey(ha => ha.AmenityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure User-Homestay relationship (Host)
            modelBuilder.Entity<Homestay>()
                .HasOne(h => h.Host)
                .WithMany(u => u.Homestays)
                .HasForeignKey(h => h.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-Booking relationship with explicit property mapping
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .Property(b => b.UserId)
                .HasColumnName("UserId");

            // Configure User-Payment relationship with explicit property mapping
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .HasPrincipalKey(u => u.Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(p => p.UserId)
                .HasColumnName("UserId");

            // Configure Booking-Promotion relationship
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Promotion)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PromotionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Promotion-User relationship
            modelBuilder.Entity<WebHSPromotion>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure HomestayPricing relationship
            modelBuilder.Entity<HomestayPricing>()
                .HasOne(hp => hp.Homestay)
                .WithMany(h => h.PricingRules)
                .HasForeignKey(hp => hp.HomestayId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Homestay)
                .WithMany()
                .HasForeignKey(m => m.HomestayId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Booking)
                .WithMany()
                .HasForeignKey(m => m.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Conversation relationships
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.LastMessageSender)
                .WithMany()
                .HasForeignKey(c => c.LastMessageSenderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Homestay)
                .WithMany()
                .HasForeignKey(c => c.HomestayId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Message-Conversation relationship
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================================================
            // CẤU HÌNH DATA TYPES & PRECISION
            // =================================================================================

            // Explicitly configure User.Id as primary key
            modelBuilder.Entity<WebHSUser>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<WebHSUser>()
                .Property(u => u.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            // Configure decimal precision for coordinates
            modelBuilder.Entity<Homestay>()
                .Property(h => h.Latitude)
                .HasColumnType("decimal(10,8)");

            modelBuilder.Entity<Homestay>()
                .Property(h => h.Longitude)
                .HasColumnType("decimal(11,8)");

            // Configure decimal precision for pricing
            modelBuilder.Entity<HomestayPricing>()
                .Property(hp => hp.PricePerNight)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Homestay>()
                .Property(h => h.PricePerNight)
                .HasColumnType("decimal(10,2)");

            // Configure decimal precision for payments and bookings
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Booking>()                .Property(b => b.DiscountAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.FinalAmount)
                .HasColumnType("decimal(10,2)");

            // =================================================================================
            // TỐI ƯU HÓA INDEXES - PERFORMANCE CRITICAL
            // =================================================================================

            // HOMESTAY INDEXES - Search & Filter Performance
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.City)
                .HasDatabaseName("IX_Homestays_City");

            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.IsActive)
                .HasDatabaseName("IX_Homestays_IsActive");

            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.IsApproved)
                .HasDatabaseName("IX_Homestays_IsApproved");

            // Composite index for active and approved homestays
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => new { h.IsActive, h.IsApproved })
                .HasDatabaseName("IX_Homestays_Active_Approved");

            // Geographic search optimization
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => new { h.Latitude, h.Longitude })
                .HasDatabaseName("IX_Homestays_Coordinates");

            // Host performance index
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => new { h.HostId, h.IsActive })
                .HasDatabaseName("IX_Homestays_Host_Active");

            // Price range search
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => h.PricePerNight)
                .HasDatabaseName("IX_Homestays_Price");

            // Full-text search preparation
            modelBuilder.Entity<Homestay>()
                .HasIndex(h => new { h.City, h.State, h.Country })
                .HasDatabaseName("IX_Homestays_Location_Full");

            // BOOKING INDEXES - Transaction Performance
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.UserId, b.Status })
                .HasDatabaseName("IX_Bookings_User_Status");

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.HomestayId, b.Status })
                .HasDatabaseName("IX_Bookings_Homestay_Status");

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.CheckInDate, b.CheckOutDate })
                .HasDatabaseName("IX_Bookings_DateRange");

            // Revenue reporting optimization
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.CreatedAt, b.Status, b.FinalAmount })
                .HasDatabaseName("IX_Bookings_Revenue_Report");

            // PAYMENT INDEXES - Financial Performance
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.UserId, p.Status })
                .HasDatabaseName("IX_Payments_User_Status");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.BookingId, p.Status })
                .HasDatabaseName("IX_Payments_Booking_Status");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique()
                .HasDatabaseName("IX_Payments_TransactionId_Unique");

            // Financial reporting
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.CreatedAt, p.Status, p.PaymentMethod })
                .HasDatabaseName("IX_Payments_Financial_Report");

            // MESSAGE INDEXES - Real-time Communication
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId, m.IsRead })
                .HasDatabaseName("IX_Messages_Sender_Receiver_Read");

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ConversationId, m.SentAt })
                .HasDatabaseName("IX_Messages_Conversation_Time");

            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SentAt)
                .HasDatabaseName("IX_Messages_SentAt");

            // Unread messages optimization
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ReceiverId, m.IsRead, m.SentAt })
                .HasDatabaseName("IX_Messages_Unread_Receiver");

            // CONVERSATION INDEXES
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique()
                .HasDatabaseName("IX_Conversations_Users_Unique");

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.LastMessageAt)
                .HasDatabaseName("IX_Conversations_LastMessage");

            // USER NOTIFICATION INDEXES
            modelBuilder.Entity<UserNotification>()
                .HasIndex(un => new { un.UserId, un.IsRead, un.CreatedAt })
                .HasDatabaseName("IX_UserNotifications_User_Read_Time");

            // HOMESTAY PRICING INDEXES
            modelBuilder.Entity<HomestayPricing>()
                .HasIndex(hp => new { hp.HomestayId, hp.Date })
                .IsUnique()
                .HasDatabaseName("IX_HomestayPricing_Homestay_Date_Unique");

            // BLOCKED DATES INDEXES
            modelBuilder.Entity<BlockedDate>()
                .HasIndex(bd => new { bd.HomestayId, bd.Date })
                .IsUnique()
                .HasDatabaseName("IX_BlockedDates_Homestay_Date_Unique");

            // PROMOTION INDEXES
            modelBuilder.Entity<WebHSPromotion>()
                .HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Promotions_Code_Unique");

            modelBuilder.Entity<WebHSPromotion>()
                .HasIndex(p => new { p.IsActive, p.StartDate, p.EndDate })
                .HasDatabaseName("IX_Promotions_Active_Period");            // USER INDEXES - Authentication & Profile
            modelBuilder.Entity<WebHSUser>()
                .HasIndex(u => new { u.IsActive, u.CreatedAt })
                .HasDatabaseName("IX_Users_Active_Created");

            // =================================================================================
            // UNIQUE CONSTRAINTS
            // =================================================================================

            // Ensure unique promotion codes
            modelBuilder.Entity<WebHSPromotion>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // Ensure unique conversation between two users
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique();

            // Ensure unique pricing per homestay per date
            modelBuilder.Entity<HomestayPricing>()
                .HasIndex(hp => new { hp.HomestayId, hp.Date })
                .IsUnique();

            // Ensure unique blocked dates per homestay
            modelBuilder.Entity<BlockedDate>()
                .HasIndex(bd => new { bd.HomestayId, bd.Date })
                .IsUnique();

            // Ensure unique transaction IDs
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.TransactionId)
                .IsUnique();            // =================================================================================
            // TABLE CONFIGURATION WITH CHECK CONSTRAINTS - Data Integrity
            // =================================================================================

            // Homestay table with constraints
            modelBuilder.Entity<Homestay>()
                .ToTable(t => 
                {
                    t.HasCheckConstraint("CK_Homestay_PricePerNight", "[PricePerNight] >= 0");
                    t.HasCheckConstraint("CK_Homestay_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90");
                    t.HasCheckConstraint("CK_Homestay_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180");
                    t.HasCheckConstraint("CK_Homestay_MaxGuests", "[MaxGuests] > 0");
                    t.HasCheckConstraint("CK_Homestay_Bedrooms", "[Bedrooms] >= 0");
                    t.HasCheckConstraint("CK_Homestay_Bathrooms", "[Bathrooms] >= 0");
                });

            // HomestayPricing table with constraints
            modelBuilder.Entity<HomestayPricing>()
                .ToTable(t => t.HasCheckConstraint("CK_HomestayPricing_PricePerNight", "[PricePerNight] >= 0"));

            // Booking table with constraints
            modelBuilder.Entity<Booking>()
                .ToTable(t => 
                {
                    t.HasCheckConstraint("CK_Booking_TotalAmount", "[TotalAmount] >= 0");
                    t.HasCheckConstraint("CK_Booking_FinalAmount", "[FinalAmount] >= 0");
                    t.HasCheckConstraint("CK_Booking_DiscountAmount", "[DiscountAmount] >= 0");
                    t.HasCheckConstraint("CK_Booking_NumberOfGuests", "[NumberOfGuests] > 0");
                    t.HasCheckConstraint("CK_Booking_DateRange", "[CheckOutDate] > [CheckInDate]");
                    t.HasCheckConstraint("CK_Booking_ReviewRating", "[ReviewRating] IS NULL OR ([ReviewRating] >= 1 AND [ReviewRating] <= 5)");
                });

            // Payment table with constraints
            modelBuilder.Entity<Payment>()
                .ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));

            // =================================================================================
            // PERFORMANCE OPTIMIZATION SETTINGS
            // =================================================================================            // Configure query tracking behavior for read-only scenarios
            // This will be handled at the service level for specific queries
        }

        // =================================================================================
        // OVERRIDE METHODS FOR AUDIT TRAIL
        // =================================================================================

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps before saving
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is IHasTimestamps timestampEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        timestampEntity.CreatedAt = DateTime.UtcNow;
                    }
                    timestampEntity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    // =================================================================================
    // INTERFACE FOR TIMESTAMP AUDIT
    // =================================================================================
    public interface IHasTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
