using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedmoretables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasbeenAssignedAgent",
                table: "Cooperatives",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalBundlePackagePacked = table.Column<long>(type: "bigint", nullable: false),
                    AvailableBundlePackagePacked = table.Column<long>(type: "bigint", nullable: false),
                    TotalBundlePackageRequested = table.Column<long>(type: "bigint", nullable: false),
                    TotalBundlePackageDisbursed = table.Column<long>(type: "bigint", nullable: false),
                    BundlePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmPlotDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    FarmerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude1 = table.Column<double>(type: "float", nullable: false),
                    Longitude1 = table.Column<double>(type: "float", nullable: false),
                    Latitude2 = table.Column<double>(type: "float", nullable: false),
                    Longitude2 = table.Column<double>(type: "float", nullable: false),
                    Latitude3 = table.Column<double>(type: "float", nullable: false),
                    Longitude3 = table.Column<double>(type: "float", nullable: false),
                    Latitude4 = table.Column<double>(type: "float", nullable: false),
                    Longitude4 = table.Column<double>(type: "float", nullable: false),
                    FarmLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmLga = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoordinateTakenBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoordinateTakenByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmPlotDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseInputInventories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<long>(type: "bigint", nullable: false),
                    UnitStockedUp = table.Column<long>(type: "bigint", nullable: false),
                    UnitDepleted = table.Column<long>(type: "bigint", nullable: false),
                    UnitBalance = table.Column<long>(type: "bigint", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    InventoryStockedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ManagedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseInputInventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lga = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManagedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    HasbeenAssignedManager = table.Column<bool>(type: "bit", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lga = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BundleItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseInputInventoryId = table.Column<long>(type: "bigint", nullable: false),
                    BundleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleItems_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_BundleId",
                table: "BundleItems",
                column: "BundleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleItems");

            migrationBuilder.DropTable(
                name: "FarmPlotDetails");

            migrationBuilder.DropTable(
                name: "WarehouseInputInventories");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "WarehouseUsers");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropColumn(
                name: "HasbeenAssignedAgent",
                table: "Cooperatives");
        }
    }
}
