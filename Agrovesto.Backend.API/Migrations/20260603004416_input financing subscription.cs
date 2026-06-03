using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class inputfinancingsubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputAllocations");

            migrationBuilder.DropTable(
                name: "InputTransactions");

            migrationBuilder.RenameColumn(
                name: "InputPackageId",
                table: "RepaymentSchedules",
                newName: "InputPackageSubscriptionId");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "RepaymentSchedules",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverdue",
                table: "RepaymentSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAccrued",
                table: "RepaymentSchedules",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyPaid",
                table: "RepaymentSchedules",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockLevel",
                table: "OrganizationLocations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Capacity",
                table: "OrganizationLocations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "LocationInventories",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityBefore",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityAfter",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InventoryTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "InventoryTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "InputPackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                table: "InputPackages",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "EconomicsOfProductions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InputPackageSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    SubscriberUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubscriberType = table.Column<int>(type: "int", nullable: false),
                    SubscribedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RepaymentFrequency = table.Column<int>(type: "int", nullable: false),
                    RepaymentDuration = table.Column<int>(type: "int", nullable: false),
                    RepaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepaymentGracePeriod = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepaymentMode = table.Column<int>(type: "int", nullable: false),
                    TotalFinancingValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentRequired = table.Column<bool>(type: "bit", nullable: false),
                    CommitmentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentPaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CommitmentPaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisbursedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RiskCategory = table.Column<int>(type: "int", nullable: false),
                    LastOverdueDetectedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OverdueCount = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputPackageSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputPackageSubscriptions_InputPackages_InputPackageId",
                        column: x => x.InputPackageId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputPackageSubscriptions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId");
                });

            migrationBuilder.CreateTable(
                name: "InputSubscriptionDisbursements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisbursedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DisbursedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputSubscriptionDisbursements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PenaltyPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageId = table.Column<long>(type: "bigint", nullable: false),
                    FlatFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyInterestRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxPenaltyCap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentPenalties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    RepaymentScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    FlatFeeApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPenalty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentPenalties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputPackageSubscriptionPaymentId = table.Column<long>(type: "bigint", nullable: false),
                    RepaymentScheduleId = table.Column<long>(type: "bigint", nullable: true),
                    PenaltyPortion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestPortion = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalPortion = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPaymentAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InputPackageSubscriptionPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentChannel = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputPackageSubscriptionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputPackageSubscriptionPayments_InputPackageSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "InputPackageSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentSchedules_InputPackageSubscriptionId",
                table: "RepaymentSchedules",
                column: "InputPackageSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptionPayments_SubscriptionId",
                table: "InputPackageSubscriptionPayments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptions_InputPackageId",
                table: "InputPackageSubscriptions",
                column: "InputPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptions_OrganizationId",
                table: "InputPackageSubscriptions",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepaymentSchedules_InputPackageSubscriptions_InputPackageSubscriptionId",
                table: "RepaymentSchedules",
                column: "InputPackageSubscriptionId",
                principalTable: "InputPackageSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepaymentSchedules_InputPackageSubscriptions_InputPackageSubscriptionId",
                table: "RepaymentSchedules");

            migrationBuilder.DropTable(
                name: "InputPackageSubscriptionPayments");

            migrationBuilder.DropTable(
                name: "InputSubscriptionDisbursements");

            migrationBuilder.DropTable(
                name: "PenaltyPolicies");

            migrationBuilder.DropTable(
                name: "RepaymentPenalties");

            migrationBuilder.DropTable(
                name: "SubscriptionPaymentAllocations");

            migrationBuilder.DropTable(
                name: "InputPackageSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_RepaymentSchedules_InputPackageSubscriptionId",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "IsOverdue",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "PenaltyAccrued",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "PenaltyPaid",
                table: "RepaymentSchedules");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "InterestRate",
                table: "InputPackages");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "EconomicsOfProductions");

            migrationBuilder.RenameColumn(
                name: "InputPackageSubscriptionId",
                table: "RepaymentSchedules",
                newName: "InputPackageId");

            migrationBuilder.AlterColumn<double>(
                name: "StockLevel",
                table: "OrganizationLocations",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Capacity",
                table: "OrganizationLocations",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "LocationInventories",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityBefore",
                table: "InventoryTransactions",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "QuantityAfter",
                table: "InventoryTransactions",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "Quantity",
                table: "InventoryTransactions",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "InputTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    FarmId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputTransactions_Farmers_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "Farmers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputTransactions_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InputAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerId = table.Column<long>(type: "bigint", nullable: false),
                    FarmId = table.Column<long>(type: "bigint", nullable: false),
                    InputId = table.Column<long>(type: "bigint", nullable: false),
                    InputTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    AllocatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputAllocations_Farmers_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "Farmers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputAllocations_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputAllocations_InputPackages_InputId",
                        column: x => x.InputId,
                        principalTable: "InputPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InputAllocations_InputTransactions_InputTransactionId",
                        column: x => x.InputTransactionId,
                        principalTable: "InputTransactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InputAllocations_FarmerId",
                table: "InputAllocations",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_InputAllocations_FarmId",
                table: "InputAllocations",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_InputAllocations_InputId",
                table: "InputAllocations",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_InputAllocations_InputTransactionId",
                table: "InputAllocations",
                column: "InputTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputTransactions_FarmerId",
                table: "InputTransactions",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_InputTransactions_FarmId",
                table: "InputTransactions",
                column: "FarmId");
        }
    }
}
