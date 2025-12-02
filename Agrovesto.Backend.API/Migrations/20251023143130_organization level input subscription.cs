using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class organizationlevelinputsubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "EquityStaticFee",
                table: "SubscriptionConfig",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RepaymentStaticFee",
                table: "RepaymentConfig",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "RepaymentDurationInMonths",
                table: "RepaymentConfig",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "InputSubscriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputSchemeId = table.Column<long>(type: "bigint", nullable: false),
                    ConfigurationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscribedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriberType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CooperativeId = table.Column<long>(type: "bigint", nullable: false),
                    EquityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RepaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RepaymentStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RepaymentEndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaymentProofUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputSubscriptions");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Wallets");

            migrationBuilder.AlterColumn<decimal>(
                name: "EquityStaticFee",
                table: "SubscriptionConfig",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RepaymentStaticFee",
                table: "RepaymentConfig",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<long>(
                name: "RepaymentDurationInMonths",
                table: "RepaymentConfig",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
