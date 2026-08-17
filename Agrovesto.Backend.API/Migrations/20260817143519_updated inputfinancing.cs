using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedinputfinancing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "WalletProviders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "UserWallets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestForUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_RequestForUserId",
                table: "InputSubscriptionDisbursements",
                column: "RequestForUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_RequestForUserId",
                table: "InputSubscriptionDisbursements",
                column: "RequestForUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_RequestForUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_RequestForUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "WalletProviders");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "UserWallets");

            migrationBuilder.DropColumn(
                name: "RequestForUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
