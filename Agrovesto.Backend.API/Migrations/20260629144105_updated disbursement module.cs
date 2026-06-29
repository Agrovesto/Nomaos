using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updateddisbursementmodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationProductId",
                table: "LocationInventories",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "LocationInventoryType",
                table: "LocationInventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "LocationInventories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "InventoryTransactions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "InventoryTransactions",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPartial",
                table: "InputSubscriptionDisbursements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "InputSubscriptionDisbursements",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<long>(
                name: "LocationId",
                table: "InputSubscriptionDisbursements",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "InputSubscriptionDisbursements",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "DisbursementLineItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisbursementId = table.Column<long>(type: "bigint", nullable: false),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationProductId = table.Column<long>(type: "bigint", nullable: false),
                    SubscribedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisbursedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PendingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDisbursedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationLocationId = table.Column<long>(type: "bigint", nullable: false),
                    AgentUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementLineItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DisbursementLineItems_InputPackageSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "InputPackageSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisbursementLineItems_InputSubscriptionDisbursements_DisbursementId",
                        column: x => x.DisbursementId,
                        principalTable: "InputSubscriptionDisbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_DisbursementLineItems_OrganizationLocations_OrganizationLocationId",
                        column: x => x.OrganizationLocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisbursementLineItems_OrganizationProducts_OrganizationProductId",
                        column: x => x.OrganizationProductId,
                        principalTable: "OrganizationProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "MonetaryDisbursementTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    DisbursementId = table.Column<long>(type: "bigint", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationLocationId = table.Column<long>(type: "bigint", nullable: false),
                    SubscribedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisbursedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PendingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DisbursedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonetaryDisbursementTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonetaryDisbursementTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MonetaryDisbursementTransactions_InputPackageSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "InputPackageSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonetaryDisbursementTransactions_InputSubscriptionDisbursements_DisbursementId",
                        column: x => x.DisbursementId,
                        principalTable: "InputSubscriptionDisbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_MonetaryDisbursementTransactions_OrganizationLocations_OrganizationLocationId",
                        column: x => x.OrganizationLocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_LocationId",
                table: "InputSubscriptionDisbursements",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementLineItems_DisbursementId",
                table: "DisbursementLineItems",
                column: "DisbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementLineItems_OrganizationLocationId",
                table: "DisbursementLineItems",
                column: "OrganizationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementLineItems_OrganizationProductId",
                table: "DisbursementLineItems",
                column: "OrganizationProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementLineItems_SubscriptionId",
                table: "DisbursementLineItems",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementLineItems_UserId",
                table: "DisbursementLineItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonetaryDisbursementTransactions_DisbursementId",
                table: "MonetaryDisbursementTransactions",
                column: "DisbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_MonetaryDisbursementTransactions_OrganizationLocationId",
                table: "MonetaryDisbursementTransactions",
                column: "OrganizationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MonetaryDisbursementTransactions_SubscriptionId",
                table: "MonetaryDisbursementTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MonetaryDisbursementTransactions_UserId",
                table: "MonetaryDisbursementTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_OrganizationLocations_LocationId",
                table: "InputSubscriptionDisbursements",
                column: "LocationId",
                principalTable: "OrganizationLocations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_OrganizationLocations_LocationId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories");

            migrationBuilder.DropTable(
                name: "DisbursementLineItems");

            migrationBuilder.DropTable(
                name: "MonetaryDisbursementTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_LocationId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "LocationInventoryType",
                table: "LocationInventories");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "LocationInventories");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "IsPartial",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationProductId",
                table: "LocationInventories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationInventories_OrganizationProducts_OrganizationProductId",
                table: "LocationInventories",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
