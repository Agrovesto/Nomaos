using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedmarketsaleslogger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CafeOneCustomerId",
                table: "SterlingBankWallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BelongToBusinessCode",
                table: "FarmerSalesTracker",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TransactionActualDate",
                table: "FarmerSalesTracker",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "supervisorId",
                table: "FarmerSalesTracker",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CafeOneCustomerId",
                table: "SterlingBankWallets");

            migrationBuilder.DropColumn(
                name: "BelongToBusinessCode",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "TransactionActualDate",
                table: "FarmerSalesTracker");

            migrationBuilder.DropColumn(
                name: "supervisorId",
                table: "FarmerSalesTracker");
        }
    }
}
