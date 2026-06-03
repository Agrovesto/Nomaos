using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedinputfinancingconfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "InputPackages",
                newName: "RepaymentFrequency");

            migrationBuilder.RenameColumn(
                name: "Season",
                table: "InputPackages",
                newName: "InputTarget");

            migrationBuilder.RenameColumn(
                name: "RepaymentPeriodInMonths",
                table: "InputPackages",
                newName: "RepaymentDuration");



            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "InputPackages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalCostAmount",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

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

            migrationBuilder.AddColumn<string>(
                name: "InputSeason",
                table: "InputPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelectedWalletProvisioned",
                table: "InputPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentEndDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentGracePeriod",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepaymentStartDate",
                table: "InputPackages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFinancingValue",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "WalletProviderId",
                table: "InputPackages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "UnitOfMeasure",
                table: "InputPackageItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

        

            migrationBuilder.CreateIndex(
                name: "IX_InputPackages_WalletProviderId",
                table: "InputPackages",
                column: "WalletProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages",
                column: "WalletProviderId",
                principalTable: "WalletProviders",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_WalletProviders_WalletProviderId",
                table: "InputPackages");


            migrationBuilder.DropIndex(
                name: "IX_InputPackages_WalletProviderId",
                table: "InputPackages");


            migrationBuilder.DropColumn(
                name: "AdditionalCostAmount",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentPaymentEndDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "CommitmentPaymentStartDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "InputSeason",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "IsSelectedWalletProvisioned",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentEndDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentGracePeriod",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymentStartDate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "TotalFinancingValue",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "WalletProviderId",
                table: "InputPackages");

            migrationBuilder.RenameColumn(
                name: "RepaymentFrequency",
                table: "InputPackages",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "RepaymentDuration",
                table: "InputPackages",
                newName: "RepaymentPeriodInMonths");

            migrationBuilder.RenameColumn(
                name: "InputTarget",
                table: "InputPackages",
                newName: "Season");

          

            migrationBuilder.AlterColumn<long>(
                name: "CreatedByUserId",
                table: "InputPackages",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UnitOfMeasure",
                table: "InputPackageItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

        }
    }
}
