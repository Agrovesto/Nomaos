using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedrelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedOn",
                table: "OrganizationAgents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "OrganizationAgents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "LocationInventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Agents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_MembershipId",
                table: "OrganizationMemberships",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAgents_AgentId",
                table: "OrganizationAgents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAgents_LocationId_IsPrimary",
                table: "OrganizationAgents",
                columns: new[] { "LocationId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MemberShipAgents_AgentId",
                table: "MemberShipAgents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberShipAgents_MemberShipId",
                table: "MemberShipAgents",
                column: "MemberShipId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOrganizations_FarmerId",
                table: "FarmerOrganizations",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_AgentId",
                table: "FarmerAgents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_FarmerId",
                table: "FarmerAgents",
                column: "FarmerId");

            // Cleanup potential orphaned rows that would violate the new foreign key constraints
            migrationBuilder.Sql(@"DELETE FROM FarmerOrganizations WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);");
            migrationBuilder.Sql(@"DELETE FROM FarmerCooperatives WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);");
            migrationBuilder.Sql(@"DELETE FROM FarmerAgents WHERE FarmerId IS NOT NULL AND FarmerId NOT IN (SELECT Id FROM Farmers);");

            // Cleanup orphaned organization-related rows that reference missing locations or organizations
            migrationBuilder.Sql(@"DELETE FROM OrganizationAgents WHERE LocationId IS NOT NULL AND LocationId NOT IN (SELECT Id FROM OrganizationLocations);");
            migrationBuilder.Sql(@"DELETE FROM OrganizationMemberships WHERE OrganizationId IS NOT NULL AND OrganizationId NOT IN (SELECT OrganizationId FROM BusinessOrganizations);");
            migrationBuilder.Sql(@"DELETE FROM OrganizationMemberships WHERE MembershipId IS NOT NULL AND MembershipId NOT IN (SELECT Id FROM MemberShips);");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerAgents_Agents_AgentId",
                table: "FarmerAgents",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerAgents_Farmers_FarmerId",
                table: "FarmerAgents",
                column: "FarmerId",
                principalTable: "Farmers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerCooperatives_Farmers_FarmerId",
                table: "FarmerCooperatives",
                column: "FarmerId",
                principalTable: "Farmers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerOrganizations_Farmers_FarmerId",
                table: "FarmerOrganizations",
                column: "FarmerId",
                principalTable: "Farmers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberShipAgents_Agents_AgentId",
                table: "MemberShipAgents",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberShipAgents_MemberShips_MemberShipId",
                table: "MemberShipAgents",
                column: "MemberShipId",
                principalTable: "MemberShips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_Agents_AgentId",
                table: "OrganizationAgents",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents",
                column: "LocationId",
                principalTable: "OrganizationLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMemberships_BusinessOrganizations_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMemberships_MemberShips_MembershipId",
                table: "OrganizationMemberships",
                column: "MembershipId",
                principalTable: "MemberShips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmerAgents_Agents_AgentId",
                table: "FarmerAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmerAgents_Farmers_FarmerId",
                table: "FarmerAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmerCooperatives_Farmers_FarmerId",
                table: "FarmerCooperatives");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmerOrganizations_Farmers_FarmerId",
                table: "FarmerOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberShipAgents_Agents_AgentId",
                table: "MemberShipAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_MemberShipAgents_MemberShips_MemberShipId",
                table: "MemberShipAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_Agents_AgentId",
                table: "OrganizationAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_OrganizationLocations_LocationId",
                table: "OrganizationAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMemberships_BusinessOrganizations_OrganizationId",
                table: "OrganizationMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMemberships_MemberShips_MembershipId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_MembershipId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_OrganizationId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationAgents_AgentId",
                table: "OrganizationAgents");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationAgents_LocationId_IsPrimary",
                table: "OrganizationAgents");

            migrationBuilder.DropIndex(
                name: "IX_MemberShipAgents_AgentId",
                table: "MemberShipAgents");

            migrationBuilder.DropIndex(
                name: "IX_MemberShipAgents_MemberShipId",
                table: "MemberShipAgents");

            migrationBuilder.DropIndex(
                name: "IX_FarmerOrganizations_FarmerId",
                table: "FarmerOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives");

            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_AgentId",
                table: "FarmerAgents");

            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_FarmerId",
                table: "FarmerAgents");

            migrationBuilder.DropColumn(
                name: "DeactivatedOn",
                table: "OrganizationAgents");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "OrganizationAgents");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "LocationInventories");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Agents");
        }
    }
}
