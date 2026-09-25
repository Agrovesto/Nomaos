using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedrejectionstatetoinputdisbursementtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RejectedOn",
                table: "InputSubscriptionDisbursements",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RejectedOn",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "InputSubscriptionDisbursements");
        }
    }
}
