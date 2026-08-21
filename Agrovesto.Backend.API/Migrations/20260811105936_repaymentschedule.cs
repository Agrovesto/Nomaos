using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class repaymentschedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "RepaymentSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOn",
                table: "RepaymentSchedules",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "RepaymentSchedules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GracePeriodDueDate",
                table: "RepaymentSchedules",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RepaymentSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupersededByScheduleId",
                table: "RepaymentSchedules",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentSchedules_SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules",
                column: "SupersededByRepaymentScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepaymentSchedules_RepaymentSchedules_SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules",
                column: "SupersededByRepaymentScheduleId",
                principalTable: "RepaymentSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepaymentSchedules_RepaymentSchedules_SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules");

            migrationBuilder.DropIndex(
                name: "IX_RepaymentSchedules_SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "GracePeriodDueDate",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "SupersededByRepaymentScheduleId",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "SupersededByScheduleId",
                table: "RepaymentSchedules");
        }
    }
}
