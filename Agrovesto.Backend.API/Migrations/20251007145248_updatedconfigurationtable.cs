using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedconfigurationtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_WalletProviderConfiguration_OrganizationConfigurationId",
                table: "WalletProviderConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_InputFinancingConfiguration_OrganizationConfigurationId",
                table: "InputFinancingConfiguration");

            migrationBuilder.RenameColumn(
                name: "SubscriptionConfigId",
                table: "InputFinancingConfiguration",
                newName: "SubscriptionConfigsId");

            migrationBuilder.RenameColumn(
                name: "RepaymentConfigId",
                table: "InputFinancingConfiguration",
                newName: "RepaymentConfigsId");

            migrationBuilder.RenameColumn(
                name: "DisbursementConfigId",
                table: "InputFinancingConfiguration",
                newName: "DisbursementConfigsId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_SubscriptionConfigId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_SubscriptionConfigsId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_RepaymentConfigId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_RepaymentConfigsId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_DisbursementConfigId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_DisbursementConfigsId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletProviderConfiguration_OrganizationConfigurationId",
                table: "WalletProviderConfiguration",
                column: "OrganizationConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_OrganizationConfigurationId",
                table: "InputFinancingConfiguration",
                column: "OrganizationConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigsId",
                table: "InputFinancingConfiguration",
                column: "DisbursementConfigsId",
                principalTable: "DisbursementConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigsId",
                table: "InputFinancingConfiguration",
                column: "RepaymentConfigsId",
                principalTable: "RepaymentConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigsId",
                table: "InputFinancingConfiguration",
                column: "SubscriptionConfigsId",
                principalTable: "SubscriptionConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigsId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigsId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigsId",
                table: "InputFinancingConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_WalletProviderConfiguration_OrganizationConfigurationId",
                table: "WalletProviderConfiguration");

            migrationBuilder.DropIndex(
                name: "IX_InputFinancingConfiguration_OrganizationConfigurationId",
                table: "InputFinancingConfiguration");

            migrationBuilder.RenameColumn(
                name: "SubscriptionConfigsId",
                table: "InputFinancingConfiguration",
                newName: "SubscriptionConfigId");

            migrationBuilder.RenameColumn(
                name: "RepaymentConfigsId",
                table: "InputFinancingConfiguration",
                newName: "RepaymentConfigId");

            migrationBuilder.RenameColumn(
                name: "DisbursementConfigsId",
                table: "InputFinancingConfiguration",
                newName: "DisbursementConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_SubscriptionConfigsId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_SubscriptionConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_RepaymentConfigsId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_RepaymentConfigId");

            migrationBuilder.RenameIndex(
                name: "IX_InputFinancingConfiguration_DisbursementConfigsId",
                table: "InputFinancingConfiguration",
                newName: "IX_InputFinancingConfiguration_DisbursementConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletProviderConfiguration_OrganizationConfigurationId",
                table: "WalletProviderConfiguration",
                column: "OrganizationConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputFinancingConfiguration_OrganizationConfigurationId",
                table: "InputFinancingConfiguration",
                column: "OrganizationConfigurationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_DisbursementConfig_DisbursementConfigId",
                table: "InputFinancingConfiguration",
                column: "DisbursementConfigId",
                principalTable: "DisbursementConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_RepaymentConfig_RepaymentConfigId",
                table: "InputFinancingConfiguration",
                column: "RepaymentConfigId",
                principalTable: "RepaymentConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputFinancingConfiguration_SubscriptionConfig_SubscriptionConfigId",
                table: "InputFinancingConfiguration",
                column: "SubscriptionConfigId",
                principalTable: "SubscriptionConfig",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
