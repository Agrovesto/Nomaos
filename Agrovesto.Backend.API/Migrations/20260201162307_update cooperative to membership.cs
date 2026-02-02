using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatecooperativetomembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId",
                table: "FarmLandDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmLandDocuments_Farms_FarmId",
                table: "FarmLandDocuments");

            migrationBuilder.DropTable(
                name: "Cooperatives");

            migrationBuilder.DropColumn(
                name: "CoordinateOverlapWithFarmerIds",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "CoordinateTakenBy",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "CoordinateTakenByUserId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByName",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByUserId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "FlaggedOn",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "FlaggedReason",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsFarmFlaggedBySystem",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "UnflaggedOn",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "UserLevel",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "UnflagFarm",
                table: "Farms",
                newName: "IsMapped");

            migrationBuilder.RenameColumn(
                name: "CooperativeId",
                table: "Farms",
                newName: "MembershipId");

            migrationBuilder.RenameColumn(
                name: "AreaInSqMetre",
                table: "Farms",
                newName: "TotalAreaInSqMetre");

            migrationBuilder.RenameColumn(
                name: "AreaInDegress",
                table: "Farms",
                newName: "TotalAreaInDegress");

            migrationBuilder.AddColumn<long>(
                name: "AgentId",
                table: "Farms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "FarmPlotDetailsId",
                table: "FarmLandDocuments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "FarmId",
                table: "FarmLandDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "FarmLandDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadedOn",
                table: "FarmLandDocuments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Farmers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CapturedBy",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CapturedOn",
                table: "FarmCoordinate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinateOverlapWithFarmerIds",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByName",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByUserId",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlaggedOn",
                table: "FarmCoordinate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedReason",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFarmFlaggedBySystem",
                table: "FarmCoordinate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MeasurementDate",
                table: "FarmCoordinate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementMode",
                table: "FarmCoordinate",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "SizeInHectares",
                table: "FarmCoordinate",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SizeInKm",
                table: "FarmCoordinate",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "UnflagFarm",
                table: "FarmCoordinate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnflaggedOn",
                table: "FarmCoordinate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberShipTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberShipTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberShips",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsLeaderAFarmer = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lga = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipLeaderFirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipLeaderMiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipLeaderLastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipLeaderGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OnBoardedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsAssignedToAgentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasbeenAssignedAgent = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnboardedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    MemberShipTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberShips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberShips_MemberShipTypes_MemberShipTypeId",
                        column: x => x.MemberShipTypeId,
                        principalTable: "MemberShipTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberShips_MemberShipTypeId",
                table: "MemberShips",
                column: "MemberShipTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId",
                table: "FarmLandDocuments",
                column: "FarmPlotDetailsId",
                principalTable: "FarmPlotDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmLandDocuments_Farms_FarmId",
                table: "FarmLandDocuments",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId",
                table: "FarmLandDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_FarmLandDocuments_Farms_FarmId",
                table: "FarmLandDocuments");

            migrationBuilder.DropTable(
                name: "MemberShips");

            migrationBuilder.DropTable(
                name: "MemberShipTypes");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "FarmLandDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedOn",
                table: "FarmLandDocuments");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Farmers");

            migrationBuilder.DropColumn(
                name: "CapturedBy",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "CapturedOn",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "CoordinateOverlapWithFarmerIds",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByName",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "FarmUnflaggedByUserId",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "FlaggedOn",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "FlaggedReason",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "IsFarmFlaggedBySystem",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "MeasurementDate",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "MeasurementMode",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "SizeInHectares",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "SizeInKm",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "UnflagFarm",
                table: "FarmCoordinate");

            migrationBuilder.DropColumn(
                name: "UnflaggedOn",
                table: "FarmCoordinate");

            migrationBuilder.RenameColumn(
                name: "TotalAreaInSqMetre",
                table: "Farms",
                newName: "AreaInSqMetre");

            migrationBuilder.RenameColumn(
                name: "TotalAreaInDegress",
                table: "Farms",
                newName: "AreaInDegress");

            migrationBuilder.RenameColumn(
                name: "MembershipId",
                table: "Farms",
                newName: "CooperativeId");

            migrationBuilder.RenameColumn(
                name: "IsMapped",
                table: "Farms",
                newName: "UnflagFarm");

            migrationBuilder.AddColumn<string>(
                name: "CoordinateOverlapWithFarmerIds",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinateTakenBy",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinateTakenByUserId",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByName",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmUnflaggedByUserId",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlaggedOn",
                table: "Farms",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedReason",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFarmFlaggedBySystem",
                table: "Farms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnflaggedOn",
                table: "Farms",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "FarmPlotDetailsId",
                table: "FarmLandDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "FarmId",
                table: "FarmLandDocuments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "UserLevel",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cooperatives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CooperativeLeaderFirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CooperativeLeaderGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CooperativeLeaderLastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CooperativeLeaderMiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasbeenAssignedAgent = table.Column<bool>(type: "bit", nullable: false),
                    IsAssignedToAgentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lga = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnBoardedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OnboardedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ward = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cooperatives", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_FarmLandDocuments_FarmPlotDetails_FarmPlotDetailsId",
                table: "FarmLandDocuments",
                column: "FarmPlotDetailsId",
                principalTable: "FarmPlotDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FarmLandDocuments_Farms_FarmId",
                table: "FarmLandDocuments",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "Id");
        }
    }
}
