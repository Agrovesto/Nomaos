using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedbundlesubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BundleSubscriptionInterests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterestedBundleId = table.Column<long>(type: "bigint", nullable: false),
                    InterestedBundleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityOfInterest = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedEquityPricePerBundle = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedTotalEquityPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalEquityPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EquityPaymentBalanceDeficit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalQuantityPaidFor = table.Column<long>(type: "bigint", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EquityPlusCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCommissionPaidOut = table.Column<bool>(type: "bit", nullable: false),
                    ShouldAddCommission = table.Column<bool>(type: "bit", nullable: false),
                    CommissionPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionPercentage = table.Column<double>(type: "float", nullable: false),
                    OfficerInChargeUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleSubscriptionInterests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BundleSubscriptionInterests");
        }
    }
}
