using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationconfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganizaionId",
                table: "BusinessOrganizations",
                newName: "OrganizationId");

            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "OrganizationConfig",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "FarmerCreditScores",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "CompletedRepayments",
                table: "FarmerCreditScores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "DefaultedRepayments",
                table: "FarmerCreditScores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalSubscriptions",
                table: "FarmerCreditScores",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CooperativeCreditScores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CooperativeId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSubscriptions = table.Column<long>(type: "bigint", nullable: false),
                    CompletedRepayments = table.Column<long>(type: "bigint", nullable: false),
                    DefaultedRepayments = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooperativeCreditScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisbursementMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisbursementTarget = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresApprovalForDisbursement = table.Column<bool>(type: "bit", nullable: false),
                    UsesInvoiceReference = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationBundlePlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CropType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinHectare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxHectare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrganizationConfigurationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationBundlePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationBundlePlans_OrganizationConfig_OrganizationConfigurationId",
                        column: x => x.OrganizationConfigurationId,
                        principalTable: "OrganizationConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepaymentMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedRepaymentPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RepaymentDurationInMonths = table.Column<long>(type: "bigint", nullable: true),
                    AllowInstallments = table.Column<bool>(type: "bit", nullable: false),
                    IsRepaymentInPercentage = table.Column<bool>(type: "bit", nullable: false),
                    IsRepaymentStaticFee = table.Column<bool>(type: "bit", nullable: false),
                    RepaymentStaticFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiresEquity = table.Column<bool>(type: "bit", nullable: false),
                    EquityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquityPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsEquityInPercentage = table.Column<bool>(type: "bit", nullable: false),
                    IsEquityStaticFee = table.Column<bool>(type: "bit", nullable: false),
                    EquityStaticFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CooperativeContributes = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationContributes = table.Column<bool>(type: "bit", nullable: false),
                    FinancialPartnerContributes = table.Column<bool>(type: "bit", nullable: false),
                    ContributionSplitJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowFarmerSelfSubscription = table.Column<bool>(type: "bit", nullable: false),
                    AllowCooperativeSubscription = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationBundleItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationBundlePlansId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationBundleItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationBundleItem_OrganizationBundlePlans_OrganizationBundlePlansId",
                        column: x => x.OrganizationBundlePlansId,
                        principalTable: "OrganizationBundlePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSubscriptions",
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
                    table.PrimaryKey("PK_OrganizationSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSubscriptions_OrganizationBundlePlans_OrganizationBundlePlansId",
                        column: x => x.OrganizationBundlePlansId,
                        principalTable: "OrganizationBundlePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InputFinancingConfiguration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionConfigId = table.Column<int>(type: "int", nullable: false),
                    DisbursementConfigId = table.Column<int>(type: "int", nullable: false),
                    RepaymentConfigId = table.Column<int>(type: "int", nullable: false),
                    BundleEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    OrganizationConfigurationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputFinancingConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigId",
                        column: x => x.DisbursementConfigId,
                        principalTable: "DisbursementConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputFinancingConfiguration_OrganizationConfig_OrganizationConfigurationId",
                        column: x => x.OrganizationConfigurationId,
                        principalTable: "OrganizationConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigId",
                        column: x => x.RepaymentConfigId,
                        principalTable: "RepaymentConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigId",
                        column: x => x.SubscriptionConfigId,
                        principalTable: "SubscriptionConfig",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Distributions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistributionReceiverType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistributionReceiverId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InputDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DistributedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DistributedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Distributions_OrganizationSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "OrganizationSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateIssued = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CollectedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_OrganizationSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "OrganizationSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRepayment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistributionId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_OrganizationRepayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationRepayment_Distributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "Distributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemQuantity = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationRepaymentId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountAcknowledged = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_OrganizationRepayment_OrganizationRepaymentId",
                        column: x => x.OrganizationRepaymentId,
                        principalTable: "OrganizationRepayment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationConfig_OrganizationId",
                table: "OrganizationConfig",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_ReferenceNumber",
                table: "Distributions",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributions_SubscriptionId",
                table: "Distributions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_DisbursementConfigId",
                table: "InputFinancingConfiguration",
                column: "DisbursementConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_OrganizationConfigurationId",
                table: "InputFinancingConfiguration",
                column: "OrganizationConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_RepaymentConfigId",
                table: "InputFinancingConfiguration",
                column: "RepaymentConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_SubscriptionConfigId",
                table: "InputFinancingConfiguration",
                column: "SubscriptionConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ReferenceCode",
                table: "Invoices",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBundleItem_OrganizationBundlePlansId",
                table: "OrganizationBundleItem",
                column: "OrganizationBundlePlansId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBundlePlans_OrganizationConfigurationId",
                table: "OrganizationBundlePlans",
                column: "OrganizationConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRepayment_DistributionId",
                table: "OrganizationRepayment",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSubscriptions_OrganizationBundlePlansId",
                table: "OrganizationSubscriptions",
                column: "OrganizationBundlePlansId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_OrganizationRepaymentId",
                table: "Receipts",
                column: "OrganizationRepaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationConfig_BusinessOrganizations_OrganizationId",
                table: "OrganizationConfig",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationConfig_BusinessOrganizations_OrganizationId",
                table: "OrganizationConfig");

            migrationBuilder.DropTable(
                name: "CooperativeCreditScores");

            migrationBuilder.DropTable(
                name: "InputFinancingConfiguration");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "OrganizationBundleItem");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "DisbursementConfig");

            migrationBuilder.DropTable(
                name: "RepaymentConfig");

            migrationBuilder.DropTable(
                name: "SubscriptionConfig");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "OrganizationRepayment");

            migrationBuilder.DropTable(
                name: "Distributions");

            migrationBuilder.DropTable(
                name: "OrganizationSubscriptions");

            migrationBuilder.DropTable(
                name: "OrganizationBundlePlans");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationConfig_OrganizationId",
                table: "OrganizationConfig");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "OrganizationConfig");

            migrationBuilder.DropColumn(
                name: "CompletedRepayments",
                table: "FarmerCreditScores");

            migrationBuilder.DropColumn(
                name: "DefaultedRepayments",
                table: "FarmerCreditScores");

            migrationBuilder.DropColumn(
                name: "TotalSubscriptions",
                table: "FarmerCreditScores");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "BusinessOrganizations",
                newName: "OrganizaionId");

            migrationBuilder.AlterColumn<long>(
                name: "Score",
                table: "FarmerCreditScores",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }
    }
}
