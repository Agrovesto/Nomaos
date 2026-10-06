using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveWarehouseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "warehouse");

            migrationBuilder.RenameTable(
                name: "WarehouseUsers",
                newName: "WarehouseUsers",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "Warehouses",
                newName: "Warehouses",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "WarehouseInputInventories",
                newName: "WarehouseInputInventories",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "OtherBundlePaymentCosts",
                newName: "OtherBundlePaymentCosts",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "InventoryTransactions",
                newName: "InventoryTransactions",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "InputDeliveryTrackers",
                newName: "InputDeliveryTrackers",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "BundleSubscriptionInterests",
                newName: "BundleSubscriptionInterests",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "BundleSubscriptionDisbursements",
                newName: "BundleSubscriptionDisbursements",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "Bundles",
                newName: "Bundles",
                newSchema: "warehouse");

            migrationBuilder.RenameTable(
                name: "BundleItems",
                newName: "BundleItems",
                newSchema: "warehouse");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WarehouseUsers",
                schema: "warehouse",
                newName: "WarehouseUsers");

            migrationBuilder.RenameTable(
                name: "Warehouses",
                schema: "warehouse",
                newName: "Warehouses");

            migrationBuilder.RenameTable(
                name: "WarehouseInputInventories",
                schema: "warehouse",
                newName: "WarehouseInputInventories");

            migrationBuilder.RenameTable(
                name: "OtherBundlePaymentCosts",
                schema: "warehouse",
                newName: "OtherBundlePaymentCosts");

            migrationBuilder.RenameTable(
                name: "InventoryTransactions",
                schema: "warehouse",
                newName: "InventoryTransactions");

            migrationBuilder.RenameTable(
                name: "InputDeliveryTrackers",
                schema: "warehouse",
                newName: "InputDeliveryTrackers");

            migrationBuilder.RenameTable(
                name: "BundleSubscriptionInterests",
                schema: "warehouse",
                newName: "BundleSubscriptionInterests");

            migrationBuilder.RenameTable(
                name: "BundleSubscriptionDisbursements",
                schema: "warehouse",
                newName: "BundleSubscriptionDisbursements");

            migrationBuilder.RenameTable(
                name: "Bundles",
                schema: "warehouse",
                newName: "Bundles");

            migrationBuilder.RenameTable(
                name: "BundleItems",
                schema: "warehouse",
                newName: "BundleItems");
        }
    }
}
