using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class farmmappingupdatedwithflags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoordinateOverlapWithFarmerIds",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByName",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByUserId",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmWard",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlaggedReason",
                table: "FarmPlotDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFarmFlaggedBySystem",
                table: "FarmPlotDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UnflagFarm",
                table: "FarmPlotDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnflaggedOn",
                table: "FarmPlotDetails",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FarmFlagAuditTrail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmPlotDetailsId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFlagAuditTrail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmFlagAuditTrail_FarmPlotDetails_FarmPlotDetailsId",
                        column: x => x.FarmPlotDetailsId,
                        principalTable: "FarmPlotDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmFlagAuditTrail_FarmPlotDetailsId",
                table: "FarmFlagAuditTrail",
                column: "FarmPlotDetailsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmFlagAuditTrail");

            migrationBuilder.DropColumn(
                name: "CoordinateOverlapWithFarmerIds",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByName",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByUserId",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "FarmWard",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "FlaggedReason",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "IsFarmFlaggedBySystem",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "UnflagFarm",
                table: "FarmPlotDetails");

            migrationBuilder.DropColumn(
                name: "UnflaggedOn",
                table: "FarmPlotDetails");
        }
    }
}
