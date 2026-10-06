using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveServiceProviderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "service_provider");

            migrationBuilder.RenameTable(
                name: "WalletProviders",
                newName: "WalletProviders",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "WalletProviderParameters",
                newName: "WalletProviderParameters",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "SMSProviderParameters",
                newName: "SMSProviderParameters",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "SMSProvider",
                newName: "SMSProvider",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "PaymentProviders",
                newName: "PaymentProviders",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "PaymentProviderParameters",
                newName: "PaymentProviderParameters",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "BusinessWalletProviders",
                newName: "BusinessWalletProviders",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "BusinessWalletProviderParameters",
                newName: "BusinessWalletProviderParameters",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "BusinessSMSProviders",
                newName: "BusinessSMSProviders",
                newSchema: "service_provider");

            migrationBuilder.RenameTable(
                name: "BusinessSMSProviderParameters",
                newName: "BusinessSMSProviderParameters",
                newSchema: "service_provider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WalletProviders",
                schema: "service_provider",
                newName: "WalletProviders");

            migrationBuilder.RenameTable(
                name: "WalletProviderParameters",
                schema: "service_provider",
                newName: "WalletProviderParameters");

            migrationBuilder.RenameTable(
                name: "SMSProviderParameters",
                schema: "service_provider",
                newName: "SMSProviderParameters");

            migrationBuilder.RenameTable(
                name: "SMSProvider",
                schema: "service_provider",
                newName: "SMSProvider");

            migrationBuilder.RenameTable(
                name: "PaymentProviders",
                schema: "service_provider",
                newName: "PaymentProviders");

            migrationBuilder.RenameTable(
                name: "PaymentProviderParameters",
                schema: "service_provider",
                newName: "PaymentProviderParameters");

            migrationBuilder.RenameTable(
                name: "BusinessWalletProviders",
                schema: "service_provider",
                newName: "BusinessWalletProviders");

            migrationBuilder.RenameTable(
                name: "BusinessWalletProviderParameters",
                schema: "service_provider",
                newName: "BusinessWalletProviderParameters");

            migrationBuilder.RenameTable(
                name: "BusinessSMSProviders",
                schema: "service_provider",
                newName: "BusinessSMSProviders");

            migrationBuilder.RenameTable(
                name: "BusinessSMSProviderParameters",
                schema: "service_provider",
                newName: "BusinessSMSProviderParameters");
        }
    }
}
