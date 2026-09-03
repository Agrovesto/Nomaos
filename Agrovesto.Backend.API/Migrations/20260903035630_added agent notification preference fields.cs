using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedagentnotificationpreferencefields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BulkUploadCompleted",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewFarmCreated",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewFarmerAssigned",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WalletRepaymentDue",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BulkUploadCompleted",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "NewFarmCreated",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "NewFarmerAssigned",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "WalletRepaymentDue",
                table: "UserNotificationPreferences");
        }
    }
}
