using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedIsActivetotables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MemberShipAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FarmerCooperatives",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FarmerAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BusinessWorkers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BusinessOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MemberShipAgents");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FarmerCooperatives");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FarmerAgents");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BusinessWorkers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Businesses");
        }
    }
}
