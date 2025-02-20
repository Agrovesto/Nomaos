using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedfinancingtracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BundleEquityPlusCommission",
                table: "Bundles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPrice",
                table: "Bundles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldAddCommission",
                table: "Bundles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleEquityPlusCommission",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CommissionPrice",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "ShouldAddCommission",
                table: "Bundles");
        }
    }
}
