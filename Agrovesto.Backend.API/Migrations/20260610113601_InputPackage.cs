using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class InputPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_OrganizationProducts_OrganizationProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationProductId",
                table: "InventoryTransactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DisbursedOn",
                table: "InputSubscriptionDisbursements",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "DisbursedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "InputSubscriptionDisbursements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedOn",
                table: "InputSubscriptionDisbursements",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DisbursedAmount",
                table: "InputSubscriptionDisbursements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingBalance",
                table: "InputSubscriptionDisbursements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RequestCreatedOn",
                table: "InputSubscriptionDisbursements",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedAmount",
                table: "InputSubscriptionDisbursements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "InputSubscriptionDisbursements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaidOn",
                table: "InputPackageSubscriptionPayments",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                table: "InputPackageSubscriptionPayments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "RecordedByUserId",
                table: "InputPackageSubscriptionPayments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionResponse",
                table: "InputPackageSubscriptionPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WalletProviderId",
                table: "InputPackageSubscriptionPayments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "AdditionalCostTreatmentType",
                table: "AdditionalInputPackageCosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PaymentReconciliation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliation_InputPackageSubscriptionPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "InputPackageSubscriptionPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubscriberWalletNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessWalletNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientWalletNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DebitReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderWalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    UploadedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionDocuments_InputPackageSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "InputPackageSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderWalletTransactionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderWalletTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Step = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderWalletTransactionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderWalletTransactionLogs_ProviderWalletTransactions_ProviderWalletTransactionId",
                        column: x => x.ProviderWalletTransactionId,
                        principalTable: "ProviderWalletTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_ApprovedByUserId",
                table: "InputSubscriptionDisbursements",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_DisbursedByUserId",
                table: "InputSubscriptionDisbursements",
                column: "DisbursedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_RequestByUserId",
                table: "InputSubscriptionDisbursements",
                column: "RequestByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputSubscriptionDisbursements_SubscriptionId",
                table: "InputSubscriptionDisbursements",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptionPayments_RecordedByUserId",
                table: "InputPackageSubscriptionPayments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputPackageSubscriptionPayments_WalletProviderId",
                table: "InputPackageSubscriptionPayments",
                column: "WalletProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliation_PaymentId",
                table: "PaymentReconciliation",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderWalletTransactionLogs_ProviderWalletTransactionId",
                table: "ProviderWalletTransactionLogs",
                column: "ProviderWalletTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionDocuments_SubscriptionId",
                table: "SubscriptionDocuments",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageSubscriptionPayments_AspNetUsers_RecordedByUserId",
                table: "InputPackageSubscriptionPayments",
                column: "RecordedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageSubscriptionPayments_WalletProviders_WalletProviderId",
                table: "InputPackageSubscriptionPayments",
                column: "WalletProviderId",
                principalTable: "WalletProviders",
                principalColumn: "ProviderId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_ApprovedByUserId",
                table: "InputSubscriptionDisbursements",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_DisbursedByUserId",
                table: "InputSubscriptionDisbursements",
                column: "DisbursedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_RequestByUserId",
                table: "InputSubscriptionDisbursements",
                column: "RequestByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputSubscriptionDisbursements_InputPackageSubscriptions_SubscriptionId",
                table: "InputSubscriptionDisbursements",
                column: "SubscriptionId",
                principalTable: "InputPackageSubscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_OrganizationProducts_OrganizationProductId",
                table: "InventoryTransactions",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageSubscriptionPayments_AspNetUsers_RecordedByUserId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageSubscriptionPayments_WalletProviders_WalletProviderId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_ApprovedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_DisbursedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_AspNetUsers_RequestByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropForeignKey(
                name: "FK_InputSubscriptionDisbursements_InputPackageSubscriptions_SubscriptionId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_OrganizationProducts_OrganizationProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "PaymentReconciliation");

            migrationBuilder.DropTable(
                name: "ProviderWalletTransactionLogs");

            migrationBuilder.DropTable(
                name: "SubscriptionDocuments");

            migrationBuilder.DropTable(
                name: "ProviderWalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_ApprovedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_DisbursedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_RequestByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_InputSubscriptionDisbursements_SubscriptionId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropIndex(
                name: "IX_InputPackageSubscriptionPayments_RecordedByUserId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropIndex(
                name: "IX_InputPackageSubscriptionPayments_WalletProviderId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "ApprovedOn",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "DisbursedAmount",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RemainingBalance",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RequestByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RequestCreatedOn",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RequestedAmount",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "InputSubscriptionDisbursements");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "RecordedByUserId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "TransactionResponse",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "WalletProviderId",
                table: "InputPackageSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "AdditionalCostTreatmentType",
                table: "AdditionalInputPackageCosts");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationProductId",
                table: "InventoryTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DisbursedOn",
                table: "InputSubscriptionDisbursements",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisbursedByUserId",
                table: "InputSubscriptionDisbursements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "InputSubscriptionDisbursements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaidOn",
                table: "InputPackageSubscriptionPayments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_OrganizationProducts_OrganizationProductId",
                table: "InventoryTransactions",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
