using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class madestatedetailsnullableonfarmertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Lga",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Farms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaSize = table.Column<double>(type: "float", nullable: false),
                    LocationDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnershipType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmerUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CooperativeId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: false),
                    MappedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmLga = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmWard = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoordinateTakenBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoordinateTakenByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaInDegress = table.Column<double>(type: "float", nullable: false),
                    AreaInSqMetre = table.Column<double>(type: "float", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlaggedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFarmFlaggedBySystem = table.Column<bool>(type: "bit", nullable: false),
                    FlaggedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoordinateOverlapWithFarmerIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnflagFarm = table.Column<bool>(type: "bit", nullable: false),
                    FarmUnflaggedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FarmUnflaggedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnflaggedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CooperativeId = table.Column<long>(type: "bigint", nullable: false),
                    FarmerUserid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllocatedToType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllocatedArea = table.Column<double>(type: "float", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FarmId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmAllocations_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmCoordinate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    FarmId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmCoordinate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmCoordinate_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmMappingAuditTrails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmMappingAuditTrails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmMappingAuditTrails_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmAllocations_FarmId",
                table: "FarmAllocations",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmCoordinate_FarmId",
                table: "FarmCoordinate",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmMappingAuditTrails_FarmId",
                table: "FarmMappingAuditTrails",
                column: "FarmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmAllocations");

            migrationBuilder.DropTable(
                name: "FarmCoordinate");

            migrationBuilder.DropTable(
                name: "FarmMappingAuditTrails");

            migrationBuilder.DropTable(
                name: "Farms");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Lga",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
