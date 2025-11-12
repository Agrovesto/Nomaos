using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class inputfinancingscheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributions_OrganizationSubscriptions_SubscriptionId",
                table: "Distributions");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_OrganizationSubscriptions_SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_OrganizationRepayment_OrganizationRepaymentId",
                table: "Receipts");

            migrationBuilder.DropTable(
                name: "OrganizationRepayment");

            migrationBuilder.DropTable(
                name: "OrganizationSubscriptions");

            migrationBuilder.RenameColumn(
                name: "OrganizationRepaymentId",
                table: "Receipts",
                newName: "SubscriptionRepaymentsId");

            migrationBuilder.RenameIndex(
                name: "IX_Receipts_OrganizationRepaymentId",
                table: "Receipts",
                newName: "IX_Receipts_SubscriptionRepaymentsId");

            migrationBuilder.RenameColumn(
                name: "SubscriptionId",
                table: "Invoices",
                newName: "SubscriptionsId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_SubscriptionId",
                table: "Invoices",
                newName: "IX_Invoices_SubscriptionsId");

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationCode",
                table: "InputFinancingConfiguration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "InputFinancingConfiguration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InputFinancingSchemes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Commodity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeneficiaryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalArea = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FundingSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarehouseId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputFinancingConfigurationId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UnitValuePerHectare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RepaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RepaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubscriptionOpenDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubscriptionCloseDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputFinancingSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionRepayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistributionsId = table.Column<long>(type: "bigint", nullable: false),
                    PaidByType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProduceDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionRepayments_Distributions_DistributionsId",
                        column: x => x.DistributionsId,
                        principalTable: "Distributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestingForFarmerId = table.Column<long>(type: "bigint", nullable: true),
                    InitiatedByCooperativeId = table.Column<long>(type: "bigint", nullable: true),
                    BeneficiaryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: false),
                    BundlePlanId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationBundlePlansId = table.Column<long>(type: "bigint", nullable: false),
                    InputDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EquityContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EquityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PartnerContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubscriptionInitiatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionApprovedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubscriptionApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_OrganizationBundlePlans_OrganizationBundlePlansId",
                        column: x => x.OrganizationBundlePlansId,
                        principalTable: "OrganizationBundlePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchemeTarget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputFinancingSchemeId = table.Column<long>(type: "bigint", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemeTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemeTarget_InputFinancingSchemes_InputFinancingSchemeId",
                        column: x => x.InputFinancingSchemeId,
                        principalTable: "InputFinancingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchemeTarget_InputFinancingSchemeId",
                table: "SchemeTarget",
                column: "InputFinancingSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRepayments_DistributionsId",
                table: "SubscriptionRepayments",
                column: "DistributionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationBundlePlansId",
                table: "Subscriptions",
                column: "OrganizationBundlePlansId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_Subscriptions_SubscriptionId",
                table: "Distributions",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionsId",
                table: "Invoices",
                column: "SubscriptionsId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_SubscriptionRepayments_SubscriptionRepaymentsId",
                table: "Receipts",
                column: "SubscriptionRepaymentsId",
                principalTable: "SubscriptionRepayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributions_Subscriptions_SubscriptionId",
                table: "Distributions");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionsId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_SubscriptionRepayments_SubscriptionRepaymentsId",
                table: "Receipts");

            migrationBuilder.DropTable(
                name: "SchemeTarget");

            migrationBuilder.DropTable(
                name: "SubscriptionRepayments");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "InputFinancingSchemes");

            migrationBuilder.DropColumn(
                name: "ConfigurationCode",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "InputFinancingConfiguration");

            migrationBuilder.RenameColumn(
                name: "SubscriptionRepaymentsId",
                table: "Receipts",
                newName: "OrganizationRepaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Receipts_SubscriptionRepaymentsId",
                table: "Receipts",
                newName: "IX_Receipts_OrganizationRepaymentId");

            migrationBuilder.RenameColumn(
                name: "SubscriptionsId",
                table: "Invoices",
                newName: "SubscriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_SubscriptionsId",
                table: "Invoices",
                newName: "IX_Invoices_SubscriptionId");

            migrationBuilder.CreateTable(
                name: "OrganizationRepayment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistributionId = table.Column<long>(type: "bigint", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidByType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProduceDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRepayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationRepayment_Distributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "Distributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationBundlePlansId = table.Column<long>(type: "bigint", nullable: false),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: false),
                    BeneficiaryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BundlePlanId = table.Column<long>(type: "bigint", nullable: true),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquityContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EquityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitiatedByCooperativeId = table.Column<long>(type: "bigint", nullable: true),
                    InputDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PartnerContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestingForFarmerId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionApprovedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubscriptionInitiatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscriptions_OrganizationBundlePlans_OrganizationBundlePlansId",
                        column: x => x.OrganizationBundlePlansId,
                        principalTable: "OrganizationBundlePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRepayment_DistributionId",
                table: "OrganizationRepayment",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_OrganizationBundlePlansId",
                table: "OrganizationSubscriptions",
                column: "OrganizationBundlePlansId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributions_OrganizationSubscriptions_SubscriptionId",
                table: "Distributions",
                column: "SubscriptionId",
                principalTable: "OrganizationSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_OrganizationSubscriptions_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId",
                principalTable: "OrganizationSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_OrganizationRepayment_OrganizationRepaymentId",
                table: "Receipts",
                column: "OrganizationRepaymentId",
                principalTable: "OrganizationRepayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
