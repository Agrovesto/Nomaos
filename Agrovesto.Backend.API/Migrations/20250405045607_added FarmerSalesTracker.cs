using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedFarmerSalesTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ManagedByUserId",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "IsAssignedToAgentId",
                table: "Cooperatives",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "FarmerSalesTracker",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectionOfficerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MarketName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MarketState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarketLga = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FarmerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CropSold = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityBroughtToMarket = table.Column<long>(type: "bigint", nullable: false),
                    QuantitySoldToday = table.Column<long>(type: "bigint", nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CropPricePerUnitSold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPriceSold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IntendedPriceToSellPerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCropTransportedToMarket = table.Column<bool>(type: "bit", nullable: false),
                    MeansOfTransport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransportCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCropPackaged = table.Column<bool>(type: "bit", nullable: false),
                    PackagingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostOfPackagingMaterial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ModeOfPayment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalTimeSpentInMarket = table.Column<long>(type: "bigint", nullable: false),
                    SalesType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerSalesTracker", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerSalesTracker");

            migrationBuilder.AlterColumn<long>(
                name: "ManagedByUserId",
                table: "Warehouses",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<long>(
                name: "IsAssignedToAgentId",
                table: "Cooperatives",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
