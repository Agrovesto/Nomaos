using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedFarmstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmerUserId",
                table: "Farms");

            migrationBuilder.AddColumn<long>(
                name: "FarmerId",
                table: "Farms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "MappingStatus",
                table: "Farms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmerId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MappingStatus",
                table: "Farms");

            migrationBuilder.AddColumn<string>(
                name: "FarmerUserId",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
