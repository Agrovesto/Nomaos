using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class changedtier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tier",
                table: "MemberShips",
                newName: "TierLevel");

            migrationBuilder.RenameColumn(
                name: "Tier",
                table: "Agents",
                newName: "TierLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TierLevel",
                table: "MemberShips",
                newName: "Tier");

            migrationBuilder.RenameColumn(
                name: "TierLevel",
                table: "Agents",
                newName: "Tier");
        }
    }
}
