using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedbundleinterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaymentDate",
                table: "BundleSubscriptionInterests",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<long>(
                name: "CooperativeId",
                table: "BundleSubscriptionInterests",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "BundleSubscriptionInterests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "BundleSubscriptionInterests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BundleEquityPrice",
                table: "Bundles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CooperativeId",
                table: "BundleSubscriptionInterests");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "BundleSubscriptionInterests");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "BundleSubscriptionInterests");

            migrationBuilder.DropColumn(
                name: "BundleEquityPrice",
                table: "Bundles");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaymentDate",
                table: "BundleSubscriptionInterests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
