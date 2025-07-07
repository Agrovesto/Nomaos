using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmvisitationlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FarmerLabourCost",
                table: "FarmVisitationActivities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FarmerTransportCost",
                table: "FarmVisitationActivities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InputCost",
                table: "FarmVisitationActivities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "InputQuantity",
                table: "FarmVisitationActivities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "InputType",
                table: "FarmVisitationActivities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                table: "FarmVisitationActivities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "FarmVisitationActivities",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmerLabourCost",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "FarmerTransportCost",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "InputCost",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "InputQuantity",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "InputType",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "OtherCost",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "FarmVisitationActivities");
        }
    }
}
