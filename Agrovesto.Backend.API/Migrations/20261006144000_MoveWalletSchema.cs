using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveWalletSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wallet");

            migrationBuilder.RenameTable(
                name: "WalletSecurities",
                newName: "WalletSecurities",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "WalletReconciliationLogs",
                newName: "WalletReconciliationLogs",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "UserWalletTransactions",
                newName: "UserWalletTransactions",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "UserWallets",
                newName: "UserWallets",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "UserWalletContexts",
                newName: "UserWalletContexts",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "SterlingBankWallets",
                newName: "SterlingBankWallets",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "ProviderWalletTransactions",
                newName: "ProviderWalletTransactions",
                newSchema: "wallet");

            migrationBuilder.RenameTable(
                name: "ProviderWalletTransactionLogs",
                newName: "ProviderWalletTransactionLogs",
                newSchema: "wallet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WalletSecurities",
                schema: "wallet",
                newName: "WalletSecurities");

            migrationBuilder.RenameTable(
                name: "WalletReconciliationLogs",
                schema: "wallet",
                newName: "WalletReconciliationLogs");

            migrationBuilder.RenameTable(
                name: "UserWalletTransactions",
                schema: "wallet",
                newName: "UserWalletTransactions");

            migrationBuilder.RenameTable(
                name: "UserWallets",
                schema: "wallet",
                newName: "UserWallets");

            migrationBuilder.RenameTable(
                name: "UserWalletContexts",
                schema: "wallet",
                newName: "UserWalletContexts");

            migrationBuilder.RenameTable(
                name: "SterlingBankWallets",
                schema: "wallet",
                newName: "SterlingBankWallets");

            migrationBuilder.RenameTable(
                name: "ProviderWalletTransactions",
                schema: "wallet",
                newName: "ProviderWalletTransactions");

            migrationBuilder.RenameTable(
                name: "ProviderWalletTransactionLogs",
                schema: "wallet",
                newName: "ProviderWalletTransactionLogs");
        }
    }
}
