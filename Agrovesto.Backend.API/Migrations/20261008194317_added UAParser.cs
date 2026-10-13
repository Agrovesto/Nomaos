using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedUAParser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AdminLoginHistories",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "AdminLoginHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "AdminLoginHistories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "AdminLoginHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "AdminLoginHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "AdminActivityLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "AdminActivityLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "AdminActivityLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "AdminActivityLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AdminActivityLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Browser",
                table: "AdminLoginHistories");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "AdminLoginHistories");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "AdminLoginHistories");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "AdminLoginHistories");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AdminActivityLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AdminLoginHistories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
