using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addednavigationproperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubscriberUserId",
                table: "InputPackageSubscriptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AddedByUserId",
                table: "InputPackageSubscriptions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "InputPackages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "InputPackages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptions_AddedByUserId",
                table: "InputPackageSubscriptions",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptions_SubscriberUserId",
                table: "InputPackageSubscriptions",
                column: "SubscriberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_CreatedById",
                table: "InputPackages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_UpdatedById",
                table: "InputPackages",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_AspNetUsers_CreatedById",
                table: "InputPackages",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_AspNetUsers_UpdatedById",
                table: "InputPackages",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageSubscriptions_AspNetUsers_AddedByUserId",
                table: "InputPackageSubscriptions",
                column: "AddedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageSubscriptions_AspNetUsers_SubscriberUserId",
                table: "InputPackageSubscriptions",
                column: "SubscriberUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_AspNetUsers_CreatedById",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_AspNetUsers_UpdatedById",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageSubscriptions_AspNetUsers_AddedByUserId",
                table: "InputPackageSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageSubscriptions_AspNetUsers_SubscriberUserId",
                table: "InputPackageSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_InputPackageSubscriptions_AddedByUserId",
                table: "InputPackageSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_InputPackageSubscriptions_SubscriberUserId",
                table: "InputPackageSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_InputPackages_CreatedById",
                table: "InputPackages");

            migrationBuilder.DropIndex(
                name: "IX_InputPackages_UpdatedById",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "InputPackages");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriberUserId",
                table: "InputPackageSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "AddedByUserId",
                table: "InputPackageSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
