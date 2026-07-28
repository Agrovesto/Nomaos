using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedindexesonfarmsquery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_AgentId",
                table: "FarmerAgents");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_FarmerId",
                table: "Farms",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_Farmers_OnBoardedOn",
                table: "Farmers",
                column: "OnBoardedOn");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_AgentId_FarmerId",
                table: "FarmerAgents",
                columns: new[] { "AgentId", "FarmerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Farms_FarmerId",
                table: "Farms");

            migrationBuilder.DropIndex(
                name: "IX_Farmers_OnBoardedOn",
                table: "Farmers");

            migrationBuilder.DropIndex(
                name: "IX_FarmerCooperatives_FarmerId",
                table: "FarmerCooperatives");

            migrationBuilder.DropIndex(
                name: "IX_FarmerAgents_AgentId_FarmerId",
                table: "FarmerAgents");

            migrationBuilder.CreateIndex(
                name: "IX_FarmerAgents_AgentId",
                table: "FarmerAgents",
                column: "AgentId");
        }
    }
}
