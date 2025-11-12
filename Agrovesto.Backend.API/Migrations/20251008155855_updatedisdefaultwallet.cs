using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedisdefaultwallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnableMultiWallet",
                table: "WalletProviderConfiguration",
                newName: "IsDefault");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultWalletProvider",
                table: "WalletProviderConfiguration",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsDefault",
                table: "WalletProviderConfiguration",
                newName: "EnableMultiWallet");

            migrationBuilder.AlterColumn<string>(
                name: "DefaultWalletProvider",
                table: "WalletProviderConfiguration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
