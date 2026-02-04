using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class upgradedfarmmapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmCoordinate_Farms_FarmId",
                table: "FarmCoordinate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmCoordinate",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "CommunityId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "Farms");

            migrationBuilder.RenameTable(
                name: "FarmCoordinate",
                newName: "FarmCoordinates");

            migrationBuilder.RenameColumn(
                name: "IsMapped",
                table: "Farms",
                newName: "IsSizeVerified");

            migrationBuilder.RenameColumn(
                name: "IsFarmFlaggedBySystem",
                table: "FarmCoordinates",
                newName: "IsSystemOverlap");

            migrationBuilder.RenameIndex(
                name: "IX_FarmCoordinate_FarmId",
                table: "FarmCoordinates",
                newName: "IX_FarmCoordinates_FarmId");

            migrationBuilder.AlterColumn<string>(
                name: "FarmWard",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FarmState",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FarmLocation",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FarmLga",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Polygon>(
                name: "FarmPolygon",
                table: "Farms",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDocumentVerified",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMappingFlagged",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMappingVerified",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MappingFlagReason",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentId",
                table: "FarmLandDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CapturedBy",
                table: "FarmCoordinates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmCoordinates",
                table: "FarmCoordinates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmCoordinates_Farms_FarmId",
                table: "FarmCoordinates",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.Sql(@"
        CREATE SPATIAL INDEX IX_Farms_FarmPolygon
        ON Farms(FarmPolygon)
        USING GEOGRAPHY_GRID
        WITH (
            GRIDS =(LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
            CELLS_PER_OBJECT = 16
        );
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmCoordinates_Farms_FarmId",
                table: "FarmCoordinates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FarmCoordinates",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "FarmPolygon",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsDocumentVerified",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsMappingFlagged",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsMappingVerified",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MappingFlagReason",
                table: "Farms");

            migrationBuilder.RenameTable(
                name: "FarmCoordinates",
                newName: "FarmCoordinate");

            migrationBuilder.RenameColumn(
                name: "IsSizeVerified",
                table: "Farms",
                newName: "IsMapped");

            migrationBuilder.RenameColumn(
                name: "IsSystemOverlap",
                table: "FarmCoordinate",
                newName: "IsFarmFlaggedBySystem");

            migrationBuilder.RenameIndex(
                name: "IX_FarmCoordinates_FarmId",
                table: "FarmCoordinate",
                newName: "IX_FarmCoordinate_FarmId");

            migrationBuilder.AlterColumn<string>(
                name: "FarmWard",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FarmState",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FarmLocation",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FarmLga",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AgentId",
                table: "Farms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CommunityId",
                table: "Farms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MembershipId",
                table: "Farms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentId",
                table: "FarmLandDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CapturedBy",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FarmCoordinate",
                table: "FarmCoordinate",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmCoordinate_Farms_FarmId",
                table: "FarmCoordinate",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.Sql(@"
        DROP INDEX IX_Farms_FarmPolygon ON Farms;
    ");
        }
    }
}
