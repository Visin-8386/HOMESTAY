using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHS.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabase_AddIndexes_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BookingId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IsRead",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId_ReceiverId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_HostId",
                table: "Homestays");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_HomestayId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_Code",
                table: "Promotions",
                newName: "IX_Promotions_Code_Unique");

            migrationBuilder.RenameIndex(
                name: "IX_HomestayPricings_HomestayId_Date",
                table: "HomestayPricings",
                newName: "IX_HomestayPricing_Homestay_Date_Unique");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_User1Id_User2Id",
                table: "Conversations",
                newName: "IX_Conversations_Users_Unique");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_LastMessageAt",
                table: "Conversations",
                newName: "IX_Conversations_LastMessage");

            migrationBuilder.RenameIndex(
                name: "IX_BlockedDates_HomestayId_Date",
                table: "BlockedDates",
                newName: "IX_BlockedDates_Homestay_Date_Unique");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerNight",
                table: "Homestays",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerLongitude",
                table: "Bookings",
                type: "decimal(11,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerLatitude",
                table: "Bookings",
                type: "decimal(10,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,6)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_User_Read_Time",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Active_Period",
                table: "Promotions",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Booking_Status",
                table: "Payments",
                columns: new[] { "BookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Financial_Report",
                table: "Payments",
                columns: new[] { "CreatedAt", "Status", "PaymentMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId_Unique",
                table: "Payments",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_User_Status",
                table: "Payments",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Amount",
                table: "Payments",
                sql: "[Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Conversation_Time",
                table: "Messages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Sender_Receiver_Read",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Unread_Receiver",
                table: "Messages",
                columns: new[] { "ReceiverId", "IsRead", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_Active_Approved",
                table: "Homestays",
                columns: new[] { "IsActive", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_Coordinates",
                table: "Homestays",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_Host_Active",
                table: "Homestays",
                columns: new[] { "HostId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_Location_Full",
                table: "Homestays",
                columns: new[] { "City", "State", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_Price",
                table: "Homestays",
                column: "PricePerNight");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_Bathrooms",
                table: "Homestays",
                sql: "[Bathrooms] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_Bedrooms",
                table: "Homestays",
                sql: "[Bedrooms] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_Latitude",
                table: "Homestays",
                sql: "[Latitude] >= -90 AND [Latitude] <= 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_Longitude",
                table: "Homestays",
                sql: "[Longitude] >= -180 AND [Longitude] <= 180");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_MaxGuests",
                table: "Homestays",
                sql: "[MaxGuests] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestay_PricePerNight",
                table: "Homestays",
                sql: "[PricePerNight] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HomestayPricing_PricePerNight",
                table: "HomestayPricings",
                sql: "[PricePerNight] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DateRange",
                table: "Bookings",
                columns: new[] { "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Homestay_Status",
                table: "Bookings",
                columns: new[] { "HomestayId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Revenue_Report",
                table: "Bookings",
                columns: new[] { "CreatedAt", "Status", "FinalAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_User_Status",
                table: "Bookings",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_DateRange",
                table: "Bookings",
                sql: "[CheckOutDate] > [CheckInDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_DiscountAmount",
                table: "Bookings",
                sql: "[DiscountAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_FinalAmount",
                table: "Bookings",
                sql: "[FinalAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_NumberOfGuests",
                table: "Bookings",
                sql: "[NumberOfGuests] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_ReviewRating",
                table: "Bookings",
                sql: "[ReviewRating] IS NULL OR ([ReviewRating] >= 1 AND [ReviewRating] <= 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_TotalAmount",
                table: "Bookings",
                sql: "[TotalAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Active_Created",
                table: "AspNetUsers",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsHost",
                table: "AspNetUsers",
                column: "IsHost");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_User_Read_Time",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_Active_Period",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Booking_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Financial_Report",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionId_Unique",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_User_Status",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Amount",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Conversation_Time",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Sender_Receiver_Read",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Unread_Receiver",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_Active_Approved",
                table: "Homestays");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_Coordinates",
                table: "Homestays");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_Host_Active",
                table: "Homestays");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_Location_Full",
                table: "Homestays");

            migrationBuilder.DropIndex(
                name: "IX_Homestays_Price",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_Bathrooms",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_Bedrooms",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_Latitude",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_Longitude",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_MaxGuests",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestay_PricePerNight",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HomestayPricing_PricePerNight",
                table: "HomestayPricings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_DateRange",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Homestay_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Revenue_Report",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_User_Status",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_DateRange",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_DiscountAmount",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_FinalAmount",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_NumberOfGuests",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_ReviewRating",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_TotalAmount",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Users_Active_Created",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsHost",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_Code_Unique",
                table: "Promotions",
                newName: "IX_Promotions_Code");

            migrationBuilder.RenameIndex(
                name: "IX_HomestayPricing_Homestay_Date_Unique",
                table: "HomestayPricings",
                newName: "IX_HomestayPricings_HomestayId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_Users_Unique",
                table: "Conversations",
                newName: "IX_Conversations_User1Id_User2Id");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_LastMessage",
                table: "Conversations",
                newName: "IX_Conversations_LastMessageAt");

            migrationBuilder.RenameIndex(
                name: "IX_BlockedDates_Homestay_Date_Unique",
                table: "BlockedDates",
                newName: "IX_BlockedDates_HomestayId_Date");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerNight",
                table: "Homestays",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FinalAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerLongitude",
                table: "Bookings",
                type: "decimal(10,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CustomerLatitude",
                table: "Bookings",
                type: "decimal(10,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsRead",
                table: "Messages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId_ReceiverId",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId" });

            migrationBuilder.CreateIndex(
                name: "IX_Homestays_HostId",
                table: "Homestays",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HomestayId",
                table: "Bookings",
                column: "HomestayId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");
        }
    }
}
