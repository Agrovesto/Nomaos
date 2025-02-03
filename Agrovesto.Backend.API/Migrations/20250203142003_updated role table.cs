using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedroletable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BusinessId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BusinessId",
                table: "AspNetRoles",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_BusinessId",
                table: "AspNetRoles",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_Businesses_BusinessId",
                table: "AspNetRoles",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Businesses_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_Businesses_BusinessId",
                table: "AspNetRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Businesses_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_BusinessId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetRoles");
        }
    }
}
