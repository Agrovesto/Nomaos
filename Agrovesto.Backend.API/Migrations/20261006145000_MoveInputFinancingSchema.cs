using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveInputFinancingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "input_financing");

            migrationBuilder.RenameTable(
                name: "SubscriptionPaymentAllocations",
                newName: "SubscriptionPaymentAllocations",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "SubscriptionDocuments",
                newName: "SubscriptionDocuments",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "SubscriptionCreditBalance",
                newName: "SubscriptionCreditBalance",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "RepaymentSchedules",
                newName: "RepaymentSchedules",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "RepaymentPlans",
                newName: "RepaymentPlans",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "RepaymentPenalties",
                newName: "RepaymentPenalties",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "PenaltyPolicies",
                newName: "PenaltyPolicies",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "PaymentReconciliation",
                newName: "PaymentReconciliation",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "MonetaryDisbursementTransactions",
                newName: "MonetaryDisbursementTransactions",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputSubscriptions",
                newName: "InputSubscriptions",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputSubscriptionDisbursements",
                newName: "InputSubscriptionDisbursements",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputPackageSubscriptions",
                newName: "InputPackageSubscriptions",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputPackageSubscriptionPayments",
                newName: "InputPackageSubscriptionPayments",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputPackages",
                newName: "InputPackages",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputPackageItems",
                newName: "InputPackageItems",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputFinancingSchemes",
                newName: "InputFinancingSchemes",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "InputFinancingCategories",
                newName: "InputFinancingCategories",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "EopCostItems",
                newName: "EopCostItems",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "EconomicsOfProductions",
                newName: "EconomicsOfProductions",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "DisbursementLineItems",
                newName: "DisbursementLineItems",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "CommitmentPlans",
                newName: "CommitmentPlans",
                newSchema: "input_financing");

            migrationBuilder.RenameTable(
                name: "AdditionalInputPackageCosts",
                newName: "AdditionalInputPackageCosts",
                newSchema: "input_financing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SubscriptionPaymentAllocations",
                schema: "input_financing",
                newName: "SubscriptionPaymentAllocations");

            migrationBuilder.RenameTable(
                name: "SubscriptionDocuments",
                schema: "input_financing",
                newName: "SubscriptionDocuments");

            migrationBuilder.RenameTable(
                name: "SubscriptionCreditBalance",
                schema: "input_financing",
                newName: "SubscriptionCreditBalance");

            migrationBuilder.RenameTable(
                name: "RepaymentSchedules",
                schema: "input_financing",
                newName: "RepaymentSchedules");

            migrationBuilder.RenameTable(
                name: "RepaymentPlans",
                schema: "input_financing",
                newName: "RepaymentPlans");

            migrationBuilder.RenameTable(
                name: "RepaymentPenalties",
                schema: "input_financing",
                newName: "RepaymentPenalties");

            migrationBuilder.RenameTable(
                name: "PenaltyPolicies",
                schema: "input_financing",
                newName: "PenaltyPolicies");

            migrationBuilder.RenameTable(
                name: "PaymentReconciliation",
                schema: "input_financing",
                newName: "PaymentReconciliation");

            migrationBuilder.RenameTable(
                name: "MonetaryDisbursementTransactions",
                schema: "input_financing",
                newName: "MonetaryDisbursementTransactions");

            migrationBuilder.RenameTable(
                name: "InputSubscriptions",
                schema: "input_financing",
                newName: "InputSubscriptions");

            migrationBuilder.RenameTable(
                name: "InputSubscriptionDisbursements",
                schema: "input_financing",
                newName: "InputSubscriptionDisbursements");

            migrationBuilder.RenameTable(
                name: "InputPackageSubscriptions",
                schema: "input_financing",
                newName: "InputPackageSubscriptions");

            migrationBuilder.RenameTable(
                name: "InputPackageSubscriptionPayments",
                schema: "input_financing",
                newName: "InputPackageSubscriptionPayments");

            migrationBuilder.RenameTable(
                name: "InputPackages",
                schema: "input_financing",
                newName: "InputPackages");

            migrationBuilder.RenameTable(
                name: "InputPackageItems",
                schema: "input_financing",
                newName: "InputPackageItems");

            migrationBuilder.RenameTable(
                name: "InputFinancingSchemes",
                schema: "input_financing",
                newName: "InputFinancingSchemes");

            migrationBuilder.RenameTable(
                name: "InputFinancingCategories",
                schema: "input_financing",
                newName: "InputFinancingCategories");

            migrationBuilder.RenameTable(
                name: "EopCostItems",
                schema: "input_financing",
                newName: "EopCostItems");

            migrationBuilder.RenameTable(
                name: "EconomicsOfProductions",
                schema: "input_financing",
                newName: "EconomicsOfProductions");

            migrationBuilder.RenameTable(
                name: "DisbursementLineItems",
                schema: "input_financing",
                newName: "DisbursementLineItems");

            migrationBuilder.RenameTable(
                name: "CommitmentPlans",
                schema: "input_financing",
                newName: "CommitmentPlans");

            migrationBuilder.RenameTable(
                name: "AdditionalInputPackageCosts",
                schema: "input_financing",
                newName: "AdditionalInputPackageCosts");
        }
    }
}
