using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class splitcommitmentandrepayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

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

            migrationBuilder.AddColumn<bool>(
                name: "RepaymeRequired",
                table: "InputPackages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CommitmentPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    CommitmentType = table.Column<int>(type: "int", nullable: false),
                    CommitmentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentPaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CommitmentPaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitmentPlans_InputPackages_InputPackageId",
                        column: x => x.InputPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    RepaymentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    RepaymentFrequency = table.Column<int>(type: "int", nullable: false),
                    RepaymentDuration = table.Column<int>(type: "int", nullable: false),
                    RepaymentMode = table.Column<int>(type: "int", nullable: false),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    RepaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepaymentGracePeriod = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    PatternType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlignToDisbursement = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestType = table.Column<int>(type: "int", nullable: false),
                    ApplyInterestOnDefault = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPenaltyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepaymentPlans_InputPackages_InputPackageId",
                        column: x => x.InputPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentPlans_InputPackageId",
                table: "CommitmentPlans",
                column: "InputPackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentPlans_InputPackageId",
                table: "RepaymentPlans",
                column: "InputPackageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitmentPlans");

            migrationBuilder.DropTable(
                name: "RepaymentPlans");

            migrationBuilder.DropColumn(
                name: "AlignToDisbursement",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "ApplyInterestOnDefault",
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
                name: "InterestType",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "PatternType",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "RepaymeRequired",
                table: "InputPackages");

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
