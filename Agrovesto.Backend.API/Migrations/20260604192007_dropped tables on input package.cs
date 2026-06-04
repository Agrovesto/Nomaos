using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class droppedtablesoninputpackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "RepaymentPlans");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "RepaymentPlans");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "RepaymentPlans");

            migrationBuilder.DropColumn(
                name: "AlignToDisbursement",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "ApplyInterestOnDefault",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentPaymentEndDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentPaymentStartDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentType",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentValue",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "DefaultPenaltyRate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "InterestRate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "InterestType",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "PatternType",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentDuration",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentEndDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentFrequency",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentGracePeriod",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentMode",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentStartDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentValue",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RiskCategory",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "InputPackages");

            migrationBuilder.RenameColumn(
                name: "RepaymeRequired",
                table: "InputPackages",
                newName: "RepaymentRequired");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RepaymentRequired",
                table: "InputPackages",
                newName: "RepaymeRequired");

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "RepaymentPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "RepaymentPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "RepaymentPlans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlignToDisbursement",
                table: "InputPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ApplyInterestOnDefault",
                table: "InputPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CommitmentPaymentEndDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CommitmentPaymentStartDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommitmentType",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CommitmentValue",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPenaltyRate",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InterestType",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PatternType",
                table: "InputPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepaymentDuration",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentEndDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepaymentFrequency",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentGracePeriod",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepaymentMode",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentStartDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RepaymentValue",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RiskCategory",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType",
                table: "InputPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
