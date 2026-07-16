using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updateoninputfinancing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmerOrganizations_FarmerId",
                table: "FarmerOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives");

            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_FarmerId",
                table: "FarmerAgents");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationCode",
                table: "FarmerOrganizations",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "CommitmentPercentage",
                table: "CommitmentPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"
    UPDATE AspNetUsers
    SET PhoneNumber = NULL
    WHERE PhoneNumber = '';
");

            migrationBuilder.AddColumn<bool>(
                name: "IsLeaderProfileComplete",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOrganizations_FarmerId_OrganizationCode",
                table: "FarmerOrganizations",
                columns: new[] { "FarmerId", "OrganizationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerCooperatives_FarmerId_CooperativeId",
                table: "FarmerCooperatives",
                columns: new[] { "FarmerId", "CooperativeId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_FarmerId_AgentId",
                table: "FarmerAgents",
                columns: new[] { "FarmerId", "AgentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmerOrganizations_FarmerId_OrganizationCode",
                table: "FarmerOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_FarmerCooperatives_FarmerId_CooperativeId",
                table: "FarmerCooperatives");

            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_FarmerId_AgentId",
                table: "FarmerAgents");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CommitmentPercentage",
                table: "CommitmentPlans");

            migrationBuilder.DropColumn(
                name: "IsLeaderProfileComplete",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationCode",
                table: "FarmerOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmerOrganizations_FarmerId",
                table: "FarmerOrganizations",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_FarmerId",
                table: "FarmerAgents",
                column: "FarmerId");
        }
    }
}
