using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class createdorganizationentitties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "WarehouseUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Traders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "SubAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "InputDeliveryTrackers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmVisitationActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportingImageUrl",
                table: "FarmVisitationActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmerSalesTracker",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmerImages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "FarmActivityTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Cooperatives",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "BusinessWorkers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "BundleSubscriptionInterests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "BundleSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Bundles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "WarehouseUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Traders");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "SubAccounts");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "InputDeliveryTrackers");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "SupportingImageUrl",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Farmers");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmerImages");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "FarmActivityTypes");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Cooperatives");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "BusinessWorkers");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "BundleSubscriptionInterests");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "BundleSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Agents");
        }
    }
}
