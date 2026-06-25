using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedcreditwalletid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreditWalletId",
                table: "UserWalletTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "WalletReconciliationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebitReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FlaggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletReconciliationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserWalletTransactions_CreditWalletId",
                table: "UserWalletTransactions",
                column: "CreditWalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions",
                column: "CreditWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWalletTransactions_UserWallets_CreditWalletId",
                table: "UserWalletTransactions");

            migrationBuilder.DropTable(
                name: "WalletReconciliationLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserWalletTransactions_CreditWalletId",
                table: "UserWalletTransactions");

            migrationBuilder.DropColumn(
                name: "CreditWalletId",
                table: "UserWalletTransactions");
        }
    }
}
