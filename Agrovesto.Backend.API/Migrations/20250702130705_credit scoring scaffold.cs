using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class creditscoringscaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditScoreWeights",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepaymentWeight = table.Column<long>(type: "bigint", nullable: false),
                    SubcriptionWeight = table.Column<long>(type: "bigint", nullable: false),
                    TimelinessWeight = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditScoreWeights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmerCreditScores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<long>(type: "bigint", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerCreditScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterestedBundleId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaymentDueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AmountToRepay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentBreakdown",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepaymentId = table.Column<long>(type: "bigint", nullable: false),
                    RepaymentMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountOrEquivalent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentBreakdown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepaymentBreakdown_Repayments_RepaymentId",
                        column: x => x.RepaymentId,
                        principalTable: "Repayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentBreakdown_RepaymentId",
                table: "RepaymentBreakdown",
                column: "RepaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditScoreWeights");

            migrationBuilder.DropTable(
                name: "FarmerCreditScores");

            migrationBuilder.DropTable(
                name: "RepaymentBreakdown");

            migrationBuilder.DropTable(
                name: "Repayments");
        }
    }
}
