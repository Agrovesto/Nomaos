using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedtheactivitieslogwithotherdetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityType",
                table: "ActivityLogs",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "ActionPerformeed",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                table: "ActivityLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AffectedRecord",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Device",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModuleAccessed",
                table: "ActivityLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionPerformeed",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "AffectedRecord",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Device",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "ModuleAccessed",
                table: "ActivityLogs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ActivityLogs",
                newName: "ActivityType");
        }
    }
}
