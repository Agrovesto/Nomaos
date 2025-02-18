using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class modifiedinventoryandbundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "WarehouseInputInventories",
                newName: "WarehouseId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "WarehouseInputInventories",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "UnitAdded",
                table: "WarehouseInputInventories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "Bundles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Bundles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                table: "Bundles",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "UnitAdded",
                table: "WarehouseInputInventories");

            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Bundles");

            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "WarehouseInputInventories",
                newName: "Unit");

            migrationBuilder.AlterColumn<string>(
                name: "Price",
                table: "WarehouseInputInventories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
