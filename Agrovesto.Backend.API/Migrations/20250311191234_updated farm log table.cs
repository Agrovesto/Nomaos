using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmlogtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "FarmVisitationActivities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "CooperativeId",
                table: "FarmVisitationActivities",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OfficerName",
                table: "FarmVisitationActivities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "FarmActivityTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "CooperativeId",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "OfficerName",
                table: "FarmVisitationActivities");

            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "FarmActivityTypes");
        }
    }
}
