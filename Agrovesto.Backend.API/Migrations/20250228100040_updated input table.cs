using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedinputtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveredTo",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InputComingFrom",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransitStatus",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredTo",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "InputComingFrom",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "TransitStatus",
                table: "WarehouseInputInventories");
        }
    }
}
