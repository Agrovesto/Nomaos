using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmplotdetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AreaInDegress",
                table: "FarmPlotDetails",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AreaInHectare",
                table: "FarmPlotDetails",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AreaInSqMetre",
                table: "FarmPlotDetails",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaInDegress",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "AreaInHectare",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "AreaInSqMetre",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "FarmPlotDetails");
        }
    }
}
