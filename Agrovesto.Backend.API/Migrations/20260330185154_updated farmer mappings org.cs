using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmermappingsorg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "FarmerOrganizations");

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "OrganizationMemberships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "OrganizationMemberships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "OrganizationAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "OrganizationAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "MemberShipAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "MemberShipAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContextType",
                table: "FarmerSalesTracker",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "FarmerSalesTracker",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "FarmerOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedOn",
                table: "FarmerOrganizations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "FarmerOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "FarmerCooperatives",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedOn",
                table: "FarmerCooperatives",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "FarmerCooperatives",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmerCooperatives",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedByUserId",
                table: "FarmerAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedOn",
                table: "FarmerAgents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "FarmerAgents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmerAgents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "OrganizationAgents");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "OrganizationAgents");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "MemberShipAgents");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "MemberShipAgents");

            migrationBuilder.DropColumn(
                name: "ContextType",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "FarmerOrganizations");

            migrationBuilder.DropColumn(
                name: "DeactivatedOn",
                table: "FarmerOrganizations");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "FarmerOrganizations");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "FarmerCooperatives");

            migrationBuilder.DropColumn(
                name: "DeactivatedOn",
                table: "FarmerCooperatives");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "FarmerCooperatives");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmerCooperatives");

            migrationBuilder.DropColumn(
                name: "DeactivatedByUserId",
                table: "FarmerAgents");

            migrationBuilder.DropColumn(
                name: "DeactivatedOn",
                table: "FarmerAgents");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "FarmerAgents");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmerAgents");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "FarmerOrganizations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
