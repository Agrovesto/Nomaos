using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedinputfinancingpackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputAllocations_Inputs_InputId",
                table: "InputAllocations");

            migrationBuilder.DropTable(
                name: "Inputs");

            migrationBuilder.DropColumn(
                name: "InventoryCode",
                table: "LocationInventories");

            migrationBuilder.AddColumn<long>(
                name: "OrganizationProductId",
                table: "LocationInventories",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EconomicsOfProductions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Crop = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LandSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedYield = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedMarketPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCostOfProduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomicsOfProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomicsOfProductions_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InputFinancingCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputFinancingCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProductCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InputPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputFinancingCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    SubScriptionType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    PackageStructure = table.Column<int>(type: "int", nullable: false),
                    TotalPackageValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentRequired = table.Column<bool>(type: "bit", nullable: false),
                    CommitmentType = table.Column<int>(type: "int", nullable: false),
                    CommitmentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    RepaymentMode = table.Column<int>(type: "int", nullable: false),
                    RepaymentPeriodInMonths = table.Column<int>(type: "int", nullable: false),
                    UserWalletId = table.Column<long>(type: "bigint", nullable: false),
                    RepaymentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ParentPackageId = table.Column<long>(type: "bigint", nullable: true),
                    AllowPartialUtilization = table.Column<bool>(type: "bit", nullable: false),
                    PlannedDisbursementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InterestRatePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsInterestApplied = table.Column<bool>(type: "bit", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    EconomicsOfProductionId = table.Column<long>(type: "bigint", nullable: true),
                    IsGeneratedFromEop = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    PublishedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputPackages_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputPackages_EconomicsOfProductions_EconomicsOfProductionId",
                        column: x => x.EconomicsOfProductionId,
                        principalTable: "EconomicsOfProductions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InputPackages_InputFinancingCategory_InputFinancingCategoryId",
                        column: x => x.InputFinancingCategoryId,
                        principalTable: "InputFinancingCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputPackages_InputPackages_ParentPackageId",
                        column: x => x.ParentPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InputPackages_UserWallets_UserWalletId",
                        column: x => x.UserWalletId,
                        principalTable: "UserWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationProductCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationProducts_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationProducts_OrganizationProductCategories_OrganizationProductCategoryId",
                        column: x => x.OrganizationProductCategoryId,
                        principalTable: "OrganizationProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalInputPackageCosts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsPerUnit = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalInputPackageCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalInputPackageCosts_InputPackages_InputPackageId",
                        column: x => x.InputPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EopCostItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EconomicsOfProductionId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrganizationProductId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EopCostItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EopCostItems_EconomicsOfProductions_EconomicsOfProductionId",
                        column: x => x.EconomicsOfProductionId,
                        principalTable: "EconomicsOfProductions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EopCostItems_OrganizationProducts_OrganizationProductId",
                        column: x => x.OrganizationProductId,
                        principalTable: "OrganizationProducts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InputPackageItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverrideCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputPackageItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputPackageItem_InputPackages_InputPackageId",
                        column: x => x.InputPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                        column: x => x.OrganizationProductId,
                        principalTable: "OrganizationProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationInventories_OrganizationProductId",
                table: "LocationInventories",
                column: "OrganizationProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalInputPackageCosts_InputPackageId",
                table: "AdditionalInputPackageCosts",
                column: "InputPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomicsOfProductions_OrganizationId",
                table: "EconomicsOfProductions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EopCostItems_EconomicsOfProductionId",
                table: "EopCostItems",
                column: "EconomicsOfProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_EopCostItems_OrganizationProductId",
                table: "EopCostItems",
                column: "OrganizationProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageItem_InputPackageId",
                table: "InputPackageItem",
                column: "InputPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageItem_OrganizationProductId",
                table: "InputPackageItem",
                column: "OrganizationProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_EconomicsOfProductionId",
                table: "InputPackages",
                column: "EconomicsOfProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_InputFinancingCategoryId",
                table: "InputPackages",
                column: "InputFinancingCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_OrganizationId",
                table: "InputPackages",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_ParentPackageId",
                table: "InputPackages",
                column: "ParentPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_UserWalletId",
                table: "InputPackages",
                column: "UserWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProducts_OrganizationId",
                table: "OrganizationProducts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProducts_OrganizationProductCategoryId",
                table: "OrganizationProducts",
                column: "OrganizationProductCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputAllocations_InputPackages_InputId",
                table: "InputAllocations",
                column: "InputId",
                principalTable: "InputPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputAllocations_InputPackages_InputId",
                table: "InputAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories");

            migrationBuilder.DropTable(
                name: "AdditionalInputPackageCosts");

            migrationBuilder.DropTable(
                name: "EopCostItems");

            migrationBuilder.DropTable(
                name: "InputPackageItem");

            migrationBuilder.DropTable(
                name: "InputPackages");

            migrationBuilder.DropTable(
                name: "OrganizationProducts");

            migrationBuilder.DropTable(
                name: "EconomicsOfProductions");

            migrationBuilder.DropTable(
                name: "InputFinancingCategory");

            migrationBuilder.DropTable(
                name: "OrganizationProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_LocationInventories_OrganizationProductId",
                table: "LocationInventories");

            migrationBuilder.DropColumn(
                name: "OrganizationProductId",
                table: "LocationInventories");

            migrationBuilder.AddColumn<string>(
                name: "InventoryCode",
                table: "LocationInventories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Inputs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inputs", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_InputAllocations_Inputs_InputId",
                table: "InputAllocations",
                column: "InputId",
                principalTable: "Inputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
