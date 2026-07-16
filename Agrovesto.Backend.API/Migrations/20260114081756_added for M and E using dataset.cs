using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedforMandEusingdataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BelongToBusinessCode",
                table: "MandEFieldDefinitions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "MandEDatasets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MandEOwnerType = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandEDatasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandEChartDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MandEDatasetId = table.Column<long>(type: "bigint", nullable: false),
                    ChartType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    XKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupByKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandEChartDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MandEChartDefinitions_MandEDatasets_MandEDatasetId",
                        column: x => x.MandEDatasetId,
                        principalTable: "MandEDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MandEDataFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MandEDatasetId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsComputed = table.Column<bool>(type: "bit", nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<long>(type: "bigint", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandEDataFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MandEDataFieldDefinitions_MandEDatasets_MandEDatasetId",
                        column: x => x.MandEDatasetId,
                        principalTable: "MandEDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MandERecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MandEDatasetId = table.Column<long>(type: "bigint", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandERecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MandERecords_MandEDatasets_MandEDatasetId",
                        column: x => x.MandEDatasetId,
                        principalTable: "MandEDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MandEChartDefinitions_MandEDatasetId",
                table: "MandEChartDefinitions",
                column: "MandEDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_MandEDataFieldDefinitions_MandEDatasetId",
                table: "MandEDataFieldDefinitions",
                column: "MandEDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_MandERecords_MandEDatasetId",
                table: "MandERecords",
                column: "MandEDatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MandEChartDefinitions");

            migrationBuilder.DropTable(
                name: "MandEDataFieldDefinitions");

            migrationBuilder.DropTable(
                name: "MandERecords");

            migrationBuilder.DropTable(
                name: "MandEDatasets");

            migrationBuilder.AlterColumn<string>(
                name: "BelongToBusinessCode",
                table: "MandEFieldDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
