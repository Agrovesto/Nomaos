using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedcreditwalletidupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions");

            migrationBuilder.AlterColumn<long>(
                name: "UserWalletId",
                table: "UserWalletTransactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "CreditWalletId",
                table: "UserWalletTransactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions",
                column: "CreditWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions");

            migrationBuilder.AlterColumn<long>(
                name: "UserWalletId",
                table: "UserWalletTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CreditWalletId",
                table: "UserWalletTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions",
                column: "CreditWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
