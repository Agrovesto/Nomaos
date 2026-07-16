using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationagenttable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents");

            migrationBuilder.AlterColumn<long>(
                name: "LocationId",
                table: "OrganizationAgents",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents",
                column: "LocationId",
                principalTable: "OrganizationLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents");

            migrationBuilder.AlterColumn<long>(
                name: "LocationId",
                table: "OrganizationAgents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents",
                column: "LocationId",
                principalTable: "OrganizationLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
