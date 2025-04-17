using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class extendedmarketrecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "QuantitySoldToday",
                table: "FarmerSalesTracker",
                type: "float",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityBroughtToMarket",
                table: "FarmerSalesTracker",
                type: "float",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<double>(
                name: "LowestUnitQuntity",
                table: "FarmerSalesTracker",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOfLowestUnit",
                table: "FarmerSalesTracker",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SoldCropUnitType",
                table: "FarmerSalesTracker",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LowestUnitQuntity",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "PriceOfLowestUnit",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "SoldCropUnitType",
                table: "FarmerSalesTracker");

            migrationBuilder.AlterColumn<long>(
                name: "QuantitySoldToday",
                table: "FarmerSalesTracker",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<long>(
                name: "QuantityBroughtToMarket",
                table: "FarmerSalesTracker",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
