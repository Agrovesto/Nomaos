using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmmeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceCovered",
                table: "Farms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MappingAccuracy",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PinnedLatitude",
                table: "Farms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PinnedLongitude",
                table: "Farms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceCovered",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MappingAccuracy",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "PinnedLatitude",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "PinnedLongitude",
                table: "Farms");
        }
    }
}
