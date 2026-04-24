using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedorganizationsublocationsandinventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfCommissioning",
                table: "OrganizationLocations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOn",
                table: "OrganizationLocations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTemperatureControl",
                table: "OrganizationLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "OrganizationLocations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "LocationStatus",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "OrganizationLocations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "OrganizationLocations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ParentLocationId",
                table: "OrganizationLocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TemperatureControlTypeId",
                table: "OrganizationLocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocationInventories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    RecordedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InventoryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InventoryOwner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    QuantityType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StorageDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    QualityGrade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RecordByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationInventories_OrganizationLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureControlTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureControlTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationModules",
                columns: table => new
                {
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    ModuleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationModules", x => new { x.LocationId, x.ModuleId });
                    table.ForeignKey(
                        name: "FK_LocationModules_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationModules_OrganizationLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationStorageCategories",
                columns: table => new
                {
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    StorageCategoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationStorageCategories", x => new { x.LocationId, x.StorageCategoryId });
                    table.ForeignKey(
                        name: "FK_LocationStorageCategories_OrganizationLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "OrganizationLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationStorageCategories_StorageCategories_StorageCategoryId",
                        column: x => x.StorageCategoryId,
                        principalTable: "StorageCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_ParentLocationId",
                table: "OrganizationLocations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_TemperatureControlTypeId",
                table: "OrganizationLocations",
                column: "TemperatureControlTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationInventories_LocationId",
                table: "LocationInventories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationModules_ModuleId",
                table: "LocationModules",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationStorageCategories_StorageCategoryId",
                table: "LocationStorageCategories",
                column: "StorageCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationLocations_OrganizationLocations_ParentLocationId",
                table: "OrganizationLocations",
                column: "ParentLocationId",
                principalTable: "OrganizationLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationLocations_TemperatureControlTypes_TemperatureControlTypeId",
                table: "OrganizationLocations",
                column: "TemperatureControlTypeId",
                principalTable: "TemperatureControlTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationLocations_OrganizationLocations_ParentLocationId",
                table: "OrganizationLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationLocations_TemperatureControlTypes_TemperatureControlTypeId",
                table: "OrganizationLocations");

            migrationBuilder.DropTable(
                name: "LocationInventories");

            migrationBuilder.DropTable(
                name: "LocationModules");

            migrationBuilder.DropTable(
                name: "LocationStorageCategories");

            migrationBuilder.DropTable(
                name: "TemperatureControlTypes");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "StorageCategories");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationLocations_ParentLocationId",
                table: "OrganizationLocations");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationLocations_TemperatureControlTypeId",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "DateOfCommissioning",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "HasTemperatureControl",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "LocationStatus",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "ParentLocationId",
                table: "OrganizationLocations");

            migrationBuilder.DropColumn(
                name: "TemperatureControlTypeId",
                table: "OrganizationLocations");
        }
    }
}
