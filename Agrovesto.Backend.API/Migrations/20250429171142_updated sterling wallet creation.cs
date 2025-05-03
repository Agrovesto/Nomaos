using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedsterlingwalletcreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CafeOneCustomerId",
                table: "SterlingBankWallets",
                newName: "customerTypeId");

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "currencyId",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "walletClassificationId",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "currencyId",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "walletClassificationId",
                table: "SterlingBankWallets");

            migrationBuilder.RenameColumn(
                name: "customerTypeId",
                table: "SterlingBankWallets",
                newName: "CafeOneCustomerId");

            migrationBuilder.AlterColumn<string>(
                name: "AccountNumber",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
