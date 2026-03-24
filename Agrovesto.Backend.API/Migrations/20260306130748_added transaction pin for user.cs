using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedtransactionpinforuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionPin",
                table: "UserWallets");

            migrationBuilder.DropColumn(
                name: "Txn",
                table: "UserWallets");
        }
    }
}
