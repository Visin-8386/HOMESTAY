using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedUserIdColumns : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop unused UserId1 columns that were created by Entity Framework
            // during initial migration due to navigation property conflicts
            
            // Drop foreign key constraints first
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_UserId1",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_UserId1",
                table: "Reviews");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId1",
                table: "Reviews");

            // Drop the unused columns
            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Reviews");
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the UserId1 columns if needed to rollback
            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            // Re-create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId1",
                table: "Reviews",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId1",
                table: "Bookings",
                column: "UserId1");

            // Re-create foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_UserId1",
                table: "Bookings",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_UserId1",
                table: "Reviews",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
