using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveOnboardingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "onboarding");

            migrationBuilder.RenameTable(
                name: "Wards",
                newName: "Wards",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "UserOrganizations",
                newName: "UserOrganizations",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "UserNotificationPreferences",
                newName: "UserNotificationPreferences",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "UserDevices",
                newName: "UserDevices",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "UserBusinesses",
                newName: "UserBusinesses",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "States",
                newName: "States",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "RevokedTokens",
                newName: "RevokedTokens",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "Organizations",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationRoles",
                newName: "OrganizationRoles",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationMemberships",
                newName: "OrganizationMemberships",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationLocations",
                newName: "OrganizationLocations",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationBusinesses",
                newName: "OrganizationBusinesses",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationAgents",
                newName: "OrganizationAgents",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OrganizationAgentRoles",
                newName: "OrganizationAgentRoles",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "OneTimePasswords",
                newName: "OneTimePasswords",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "MemberShipTypes",
                newName: "MemberShipTypes",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "MemberShips",
                newName: "MemberShips",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "MemberShipAgents",
                newName: "MemberShipAgents",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "LGAs",
                newName: "LGAs",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "KYCDetails",
                newName: "KYCDetails",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "Farms",
                newName: "Farms",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmPlotDetails",
                newName: "FarmPlotDetails",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmMappingAuditTrails",
                newName: "FarmMappingAuditTrails",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmLandDocuments",
                newName: "FarmLandDocuments",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmFlagAuditTrail",
                newName: "FarmFlagAuditTrail",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "Farmers",
                newName: "Farmers",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmerOrganizations",
                newName: "FarmerOrganizations",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmerImages",
                newName: "FarmerImages",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmerCooperatives",
                newName: "FarmerCooperatives",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmerAgents",
                newName: "FarmerAgents",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmCoordinates",
                newName: "FarmCoordinates",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "FarmAllocations",
                newName: "FarmAllocations",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "BulkUploadJobs",
                newName: "BulkUploadJobs",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "onboarding");

            migrationBuilder.RenameTable(
                name: "Agents",
                newName: "Agents",
                newSchema: "onboarding");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Wards",
                schema: "onboarding",
                newName: "Wards");

            migrationBuilder.RenameTable(
                name: "UserOrganizations",
                schema: "onboarding",
                newName: "UserOrganizations");

            migrationBuilder.RenameTable(
                name: "UserNotificationPreferences",
                schema: "onboarding",
                newName: "UserNotificationPreferences");

            migrationBuilder.RenameTable(
                name: "UserDevices",
                schema: "onboarding",
                newName: "UserDevices");

            migrationBuilder.RenameTable(
                name: "UserBusinesses",
                schema: "onboarding",
                newName: "UserBusinesses");

            migrationBuilder.RenameTable(
                name: "States",
                schema: "onboarding",
                newName: "States");

            migrationBuilder.RenameTable(
                name: "RevokedTokens",
                schema: "onboarding",
                newName: "RevokedTokens");

            migrationBuilder.RenameTable(
                name: "Organizations",
                schema: "onboarding",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "OrganizationRoles",
                schema: "onboarding",
                newName: "OrganizationRoles");

            migrationBuilder.RenameTable(
                name: "OrganizationMemberships",
                schema: "onboarding",
                newName: "OrganizationMemberships");

            migrationBuilder.RenameTable(
                name: "OrganizationLocations",
                schema: "onboarding",
                newName: "OrganizationLocations");

            migrationBuilder.RenameTable(
                name: "OrganizationBusinesses",
                schema: "onboarding",
                newName: "OrganizationBusinesses");

            migrationBuilder.RenameTable(
                name: "OrganizationAgents",
                schema: "onboarding",
                newName: "OrganizationAgents");

            migrationBuilder.RenameTable(
                name: "OrganizationAgentRoles",
                schema: "onboarding",
                newName: "OrganizationAgentRoles");

            migrationBuilder.RenameTable(
                name: "OneTimePasswords",
                schema: "onboarding",
                newName: "OneTimePasswords");

            migrationBuilder.RenameTable(
                name: "MemberShipTypes",
                schema: "onboarding",
                newName: "MemberShipTypes");

            migrationBuilder.RenameTable(
                name: "MemberShips",
                schema: "onboarding",
                newName: "MemberShips");

            migrationBuilder.RenameTable(
                name: "MemberShipAgents",
                schema: "onboarding",
                newName: "MemberShipAgents");

            migrationBuilder.RenameTable(
                name: "LGAs",
                schema: "onboarding",
                newName: "LGAs");

            migrationBuilder.RenameTable(
                name: "KYCDetails",
                schema: "onboarding",
                newName: "KYCDetails");

            migrationBuilder.RenameTable(
                name: "Farms",
                schema: "onboarding",
                newName: "Farms");

            migrationBuilder.RenameTable(
                name: "FarmPlotDetails",
                schema: "onboarding",
                newName: "FarmPlotDetails");

            migrationBuilder.RenameTable(
                name: "FarmMappingAuditTrails",
                schema: "onboarding",
                newName: "FarmMappingAuditTrails");

            migrationBuilder.RenameTable(
                name: "FarmLandDocuments",
                schema: "onboarding",
                newName: "FarmLandDocuments");

            migrationBuilder.RenameTable(
                name: "FarmFlagAuditTrail",
                schema: "onboarding",
                newName: "FarmFlagAuditTrail");

            migrationBuilder.RenameTable(
                name: "Farmers",
                schema: "onboarding",
                newName: "Farmers");

            migrationBuilder.RenameTable(
                name: "FarmerOrganizations",
                schema: "onboarding",
                newName: "FarmerOrganizations");

            migrationBuilder.RenameTable(
                name: "FarmerImages",
                schema: "onboarding",
                newName: "FarmerImages");

            migrationBuilder.RenameTable(
                name: "FarmerCooperatives",
                schema: "onboarding",
                newName: "FarmerCooperatives");

            migrationBuilder.RenameTable(
                name: "FarmerAgents",
                schema: "onboarding",
                newName: "FarmerAgents");

            migrationBuilder.RenameTable(
                name: "FarmCoordinates",
                schema: "onboarding",
                newName: "FarmCoordinates");

            migrationBuilder.RenameTable(
                name: "FarmAllocations",
                schema: "onboarding",
                newName: "FarmAllocations");

            migrationBuilder.RenameTable(
                name: "BulkUploadJobs",
                schema: "onboarding",
                newName: "BulkUploadJobs");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "onboarding",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "onboarding",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "onboarding",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "onboarding",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "onboarding",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "onboarding",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "onboarding",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Agents",
                schema: "onboarding",
                newName: "Agents");
        }
    }
}
