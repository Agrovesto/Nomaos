using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedwalletsecurities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionPin",
                table: "UserWallets");

            migrationBuilder.DropColumn(
                name: "Txn",
                table: "UserWallets");

            migrationBuilder.CreateTable(
                name: "WalletSecurities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserWalletId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionPinHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Txn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PinFailedAttempts = table.Column<int>(type: "int", nullable: false),
                    PinLockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastPinChangeDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletSecurities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletSecurities_UserWallets_UserWalletId",
                        column: x => x.UserWalletId,
                        principalTable: "UserWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletSecurities_UserWalletId",
                table: "WalletSecurities",
                column: "UserWalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletSecurities");

            migrationBuilder.AddColumn<string>(
                name: "TransactionPin",
                table: "UserWallets",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Txn",
                table: "UserWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
