using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsHostColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsHost",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerCity",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerDistrict",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerFullAddress",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerHouseNumber",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerLatitude",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerLongitude",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerStreetName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerWard",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerCity",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDistrict",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFullAddress",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerHouseNumber",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerLatitude",
                table: "Bookings",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerLongitude",
                table: "Bookings",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerStreetName",
                table: "Bookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerWard",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsHost",
                table: "AspNetUsers",
                column: "IsHost");
        }
    }
}
