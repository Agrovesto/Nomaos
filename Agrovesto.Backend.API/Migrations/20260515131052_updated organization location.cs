using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationlocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LocationStatus",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "WardId",
                table: "OrganizationLocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_WardId",
                table: "OrganizationLocations",
                column: "WardId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationLocations_Wards_WardId",
                table: "OrganizationLocations",
                column: "WardId",
                principalTable: "Wards",
                principalColumn: "WardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationLocations_Wards_WardId",
                table: "OrganizationLocations");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationLocations_WardId",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "OrganizationLocations");

            migrationBuilder.AlterColumn<string>(
                name: "LocationStatus",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
