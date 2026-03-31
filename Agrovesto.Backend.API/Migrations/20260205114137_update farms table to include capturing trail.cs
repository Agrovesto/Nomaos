using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatefarmstabletoincludecapturingtrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapturedBy",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "CapturedOn",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "MeasurementDate",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "MeasurementMode",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "SizeInHectares",
                table: "FarmCoordinates");

            migrationBuilder.DropColumn(
                name: "SizeInKm",
                table: "FarmCoordinates");

            migrationBuilder.AddColumn<string>(
                name: "CapturedBy",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CapturedOn",
                table: "Farms",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MeasurementDate",
                table: "Farms",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementMode",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "SizeInHectares",
                table: "Farms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SizeInKm",
                table: "Farms",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapturedBy",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "CapturedOn",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MeasurementDate",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "MeasurementMode",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "SizeInHectares",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "SizeInKm",
                table: "Farms");

            migrationBuilder.AddColumn<string>(
                name: "CapturedBy",
                table: "FarmCoordinates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CapturedOn",
                table: "FarmCoordinates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MeasurementDate",
                table: "FarmCoordinates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementMode",
                table: "FarmCoordinates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "SizeInHectares",
                table: "FarmCoordinates",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SizeInKm",
                table: "FarmCoordinates",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
