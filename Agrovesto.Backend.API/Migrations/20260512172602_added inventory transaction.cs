using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedinventorytransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationInventoryId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationProductId = table.Column<long>(type: "bigint", nullable: false),
                    FromLocationId = table.Column<long>(type: "bigint", nullable: true),
                    ToLocationId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    QuantityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityBefore = table.Column<double>(type: "float", nullable: false),
                    QuantityAfter = table.Column<double>(type: "float", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_LocationInventories_LocationInventoryId",
                        column: x => x.LocationInventoryId,
                        principalTable: "LocationInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OrganizationLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OrganizationLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OrganizationProducts_OrganizationProductId",
                        column: x => x.OrganizationProductId,
                        principalTable: "OrganizationProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_FromLocationId",
                table: "InventoryTransactions",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_LocationInventoryId",
                table: "InventoryTransactions",
                column: "LocationInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrganizationId",
                table: "InventoryTransactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrganizationProductId",
                table: "InventoryTransactions",
                column: "OrganizationProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ToLocationId",
                table: "InventoryTransactions",
                column: "ToLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransactions");
        }
    }
}
