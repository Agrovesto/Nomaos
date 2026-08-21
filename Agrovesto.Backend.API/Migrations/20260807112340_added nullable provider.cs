using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addednullableprovider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages");

            migrationBuilder.AlterColumn<long>(
                name: "WalletProviderId",
                table: "InputPackages",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages",
                column: "WalletProviderId",
                principalTable: "WalletProviders",
                principalColumn: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages");

            migrationBuilder.AlterColumn<long>(
                name: "WalletProviderId",
                table: "InputPackages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages",
                column: "WalletProviderId",
                principalTable: "WalletProviders",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
