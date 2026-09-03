using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addeduserroletonotificationpreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotificationPreferences_UserId",
                table: "UserNotificationPreferences");

            migrationBuilder.AddColumn<int>(
                name: "UserRole",
                table: "UserNotificationPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId_UserRole",
                table: "UserNotificationPreferences",
                columns: new[] { "UserId", "UserRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotificationPreferences_UserId_UserRole",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "UserRole",
                table: "UserNotificationPreferences");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId",
                table: "UserNotificationPreferences",
                column: "UserId",
                unique: true);
        }
    }
}
