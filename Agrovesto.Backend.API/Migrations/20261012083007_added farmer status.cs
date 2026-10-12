using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedfarmerstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryCrop",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Farmers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "FarmerStatus",
                table: "Farmers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Farmers_UserId",
                table: "Farmers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Farmers_AspNetUsers_UserId",
                table: "Farmers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Farmers_AspNetUsers_UserId",
                table: "Farmers");

            migrationBuilder.DropIndex(
                name: "IX_Farmers_UserId",
                table: "Farmers");

            migrationBuilder.DropColumn(
                name: "PrimaryCrop",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "FarmerStatus",
                table: "Farmers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
