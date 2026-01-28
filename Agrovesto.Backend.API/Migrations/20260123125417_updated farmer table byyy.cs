using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmertablebyyy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BelongToCooperativeId",
                table: "Farmers");

            migrationBuilder.CreateTable(
                name: "FarmerAgents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerAgents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmerCooperatives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    CooperativeId = table.Column<long>(type: "bigint", nullable: false),
                    JoinedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OnboardedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerCooperatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KYCDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoggedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthcountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthlga = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthstate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthdate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Maidenname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Middlename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phonenumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Residentialaddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Photourl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCDetails", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerAgents");

            migrationBuilder.DropTable(
                name: "FarmerCooperatives");

            migrationBuilder.DropTable(
                name: "KYCDetails");

            migrationBuilder.AddColumn<long>(
                name: "BelongToCooperativeId",
                table: "Farmers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
