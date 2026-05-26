using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedmultitenancyarch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_EconomicsOfProductions_BusinessOrganizations_OrganizationId",
                table: "EconomicsOfProductions");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_BusinessOrganizations_OrganizationId",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_BusinessOrganizations_OrganizationId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_BusinessOrganizations_OrganizationId",
                table: "OrganizationAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationConfig_BusinessOrganizations_OrganizationId",
                table: "OrganizationConfig");

         

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationLocations_BusinessOrganizations_OrganizationId",
                table: "OrganizationLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMemberships_BusinessOrganizations_OrganizationId",
                table: "OrganizationMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationProducts_BusinessOrganizations_OrganizationId",
                table: "OrganizationProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRoles_BusinessOrganizations_OrganizationId",
                table: "OrganizationRoles");

            //migrationBuilder.DropIndex(
            //    name: "IX_OrganizationConfig_OrganizationId",
            //    table: "OrganizationConfig");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessOrganizations",
                table: "BusinessOrganizations");

            //migrationBuilder.DropColumn(
            //    name: "OrganizationId",
            //    table: "OrganizationConfig");


          

            migrationBuilder.RenameTable(
                name: "BusinessOrganizations",
                newName: "Organizations");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessOrganizations_StateId",
                table: "Organizations",
                newName: "IX_Organizations_StateId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessOrganizations_LgaId",
                table: "Organizations",
                newName: "IX_Organizations_LgaId");

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSenderName",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuspended",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeColor",
                table: "Businesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations",
                column: "OrganizationId");

            migrationBuilder.CreateTable(
                name: "BusinessBillingInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessBillingInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLicenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<long>(type: "bigint", nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationInMonths = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeactivatedReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLicenses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MigrationTrackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    RunOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationBusinesses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationBusinesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationBusinesses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationBusinesses_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemModules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    RequiresSubscription = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBusinesses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BusinessId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InvitedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBusinesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBusinesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBusinesses_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessBillingInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessPayments_BusinessBillingInvoices_BusinessBillingInvoiceId",
                        column: x => x.BusinessBillingInvoiceId,
                        principalTable: "BusinessBillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLicenseModules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessLicenseId = table.Column<long>(type: "bigint", nullable: false),
                    SystemModuleId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EnabledOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLicenseModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLicenseModules_BusinessLicenses_BusinessLicenseId",
                        column: x => x.BusinessLicenseId,
                        principalTable: "BusinessLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessLicenseModules_SystemModules_SystemModuleId",
                        column: x => x.SystemModuleId,
                        principalTable: "SystemModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLicenseModules_BusinessLicenseId",
                table: "BusinessLicenseModules",
                column: "BusinessLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLicenseModules_SystemModuleId",
                table: "BusinessLicenseModules",
                column: "SystemModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLicenses_BusinessId",
                table: "BusinessLicenses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPayments_BusinessBillingInvoiceId",
                table: "BusinessPayments",
                column: "BusinessBillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBusinesses_BusinessId",
                table: "OrganizationBusinesses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationBusinesses_OrganizationId",
                table: "OrganizationBusinesses",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinesses_BusinessId",
                table: "UserBusinesses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinesses_UserId",
                table: "UserBusinesses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EconomicsOfProductions_Organizations_OrganizationId",
                table: "EconomicsOfProductions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_Organizations_OrganizationId",
                table: "InputPackages",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Organizations_OrganizationId",
                table: "InventoryTransactions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_Organizations_OrganizationId",
                table: "OrganizationAgents",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationLocations_Organizations_OrganizationId",
                table: "OrganizationLocations",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationProducts_Organizations_OrganizationId",
                table: "OrganizationProducts",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRoles_Organizations_OrganizationId",
                table: "OrganizationRoles",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_LGAs_LgaId",
                table: "Organizations",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_States_StateId",
                table: "Organizations",
                column: "StateId",
                principalTable: "States",
                principalColumn: "StateId");

            migrationBuilder.AddForeignKey(
 name: "FK_OrganizationConfig_Organizations_OrganizationId",
 table: "OrganizationConfig",
 column: "OrganizationId",
 principalTable: "Organizations",
 principalColumn: "OrganizationId",
 onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
    name: "FK_OrganizationConfig_Organizations_OrganizationId",
    table: "OrganizationConfig");
            migrationBuilder.DropForeignKey(
                name: "FK_EconomicsOfProductions_Organizations_OrganizationId",
                table: "EconomicsOfProductions");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_Organizations_OrganizationId",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Organizations_OrganizationId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationAgents_Organizations_OrganizationId",
                table: "OrganizationAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationLocations_Organizations_OrganizationId",
                table: "OrganizationLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                table: "OrganizationMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationProducts_Organizations_OrganizationId",
                table: "OrganizationProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRoles_Organizations_OrganizationId",
                table: "OrganizationRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_LGAs_LgaId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_States_StateId",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "BusinessLicenseModules");

            migrationBuilder.DropTable(
                name: "BusinessPayments");

            migrationBuilder.DropTable(
                name: "MigrationTrackers");

            migrationBuilder.DropTable(
                name: "OrganizationBusinesses");

            migrationBuilder.DropTable(
                name: "UserBusinesses");

            migrationBuilder.DropTable(
                name: "BusinessLicenses");

            migrationBuilder.DropTable(
                name: "SystemModules");

            migrationBuilder.DropTable(
                name: "BusinessBillingInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CustomDomain",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "EmailSenderName",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "IsSuspended",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ThemeColor",
                table: "Businesses");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "BusinessOrganizations");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_StateId",
                table: "BusinessOrganizations",
                newName: "IX_BusinessOrganizations_StateId");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_LgaId",
                table: "BusinessOrganizations",
                newName: "IX_BusinessOrganizations_LgaId");

            //migrationBuilder.AddColumn<long>(
            //    name: "OrganizationId",
            //    table: "OrganizationConfig",
            //    type: "bigint",
            //    nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessOrganizations",
                table: "BusinessOrganizations",
                column: "OrganizationId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_OrganizationConfig_OrganizationId",
            //    table: "OrganizationConfig",
            //    column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations",
                column: "StateId",
                principalTable: "States",
                principalColumn: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_EconomicsOfProductions_BusinessOrganizations_OrganizationId",
                table: "EconomicsOfProductions",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_BusinessOrganizations_OrganizationId",
                table: "InputPackages",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_BusinessOrganizations_OrganizationId",
                table: "InventoryTransactions",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationAgents_BusinessOrganizations_OrganizationId",
                table: "OrganizationAgents",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationConfig_BusinessOrganizations_OrganizationId",
                table: "OrganizationConfig",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationLocations_BusinessOrganizations_OrganizationId",
                table: "OrganizationLocations",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMemberships_BusinessOrganizations_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationProducts_BusinessOrganizations_OrganizationId",
                table: "OrganizationProducts",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRoles_BusinessOrganizations_OrganizationId",
                table: "OrganizationRoles",
                column: "OrganizationId",
                principalTable: "BusinessOrganizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
