using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedfarmerorganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Farmers");

            migrationBuilder.CreateTable(
                name: "FarmerOrganizations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnBoardedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OnboardedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerOrganizations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerOrganizations");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
