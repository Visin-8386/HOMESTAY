using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReviewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_UserId1",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_AspNetUsers_UserId",
                table: "Promotions");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_UserId",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Homestays");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Homestays");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "Bookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewCreatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewIsActive",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReviewRating",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewUpdatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewCreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewIsActive",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewRating",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewUpdatedAt",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Promotions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Homestays",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Homestays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    HomestayId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Homestays_HomestayId",
                        column: x => x.HomestayId,
                        principalTable: "Homestays",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_UserId",
                table: "Promotions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HomestayId",
                table: "Reviews",
                column: "HomestayId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId1",
                table: "Reviews",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_UserId1",
                table: "Bookings",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_AspNetUsers_UserId",
                table: "Promotions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
