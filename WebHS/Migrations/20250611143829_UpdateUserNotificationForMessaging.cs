using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebHS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserNotificationForMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "UserNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedBy",
                table: "UserNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConversationId",
                table: "UserNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "UserNotifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequesterEmail",
                table: "UserNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterId",
                table: "UserNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequesterName",
                table: "UserNotifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "AcceptedBy",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "RequesterEmail",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "RequesterId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "RequesterName",
                table: "UserNotifications");
        }
    }
}
