using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedwalletandsmstosupportmultiple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaxCheckSum",
                table: "UserWallets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationCode",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NIN",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BVN",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessWalletProviders",
                columns: table => new
                {
                    BusinessWalletProviderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessWalletProviders", x => x.BusinessWalletProviderId);
                    table.ForeignKey(
                        name: "FK_BusinessWalletProviders_WalletProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "WalletProviders",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SMSProvider",
                columns: table => new
                {
                    ProviderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSProvider", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessWalletProviderParameters",
                columns: table => new
                {
                    ParameterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Env = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessWalletProviderId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessWalletProviderParameters", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_BusinessWalletProviderParameters_BusinessWalletProviders_BusinessWalletProviderId",
                        column: x => x.BusinessWalletProviderId,
                        principalTable: "BusinessWalletProviders",
                        principalColumn: "BusinessWalletProviderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSMSProviders",
                columns: table => new
                {
                    BusinessSMSProviderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    ProviderId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSMSProviders", x => x.BusinessSMSProviderId);
                    table.ForeignKey(
                        name: "FK_BusinessSMSProviders_SMSProvider_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "SMSProvider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SMSProviderParameters",
                columns: table => new
                {
                    ParameterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SMSProviderProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Env = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSProviderParameters", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_SMSProviderParameters_SMSProvider_SMSProviderProviderId",
                        column: x => x.SMSProviderProviderId,
                        principalTable: "SMSProvider",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSMSProviderParameters",
                columns: table => new
                {
                    ParameterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Env = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessSMSProviderId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSMSProviderParameters", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_BusinessSMSProviderParameters_BusinessSMSProviders_BusinessSMSProviderId",
                        column: x => x.BusinessSMSProviderId,
                        principalTable: "BusinessSMSProviders",
                        principalColumn: "BusinessSMSProviderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BVN",
                table: "AspNetUsers",
                column: "BVN",
                unique: true,
                filter: "[BVN] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NIN",
                table: "AspNetUsers",
                column: "NIN",
                unique: true,
                filter: "[NIN] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSMSProviderParameters_BusinessSMSProviderId",
                table: "BusinessSMSProviderParameters",
                column: "BusinessSMSProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSMSProviders_BusinessCode_ProviderId",
                table: "BusinessSMSProviders",
                columns: new[] { "BusinessCode", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSMSProviders_ProviderId",
                table: "BusinessSMSProviders",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWalletProviderParameters_BusinessWalletProviderId",
                table: "BusinessWalletProviderParameters",
                column: "BusinessWalletProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWalletProviders_BusinessCode_ProviderId",
                table: "BusinessWalletProviders",
                columns: new[] { "BusinessCode", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessWalletProviders_ProviderId",
                table: "BusinessWalletProviders",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSProviderParameters_SMSProviderProviderId",
                table: "SMSProviderParameters",
                column: "SMSProviderProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSMSProviderParameters");

            migrationBuilder.DropTable(
                name: "BusinessWalletProviderParameters");

            migrationBuilder.DropTable(
                name: "SMSProviderParameters");

            migrationBuilder.DropTable(
                name: "BusinessSMSProviders");

            migrationBuilder.DropTable(
                name: "BusinessWalletProviders");

            migrationBuilder.DropTable(
                name: "SMSProvider");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BVN",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NIN",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MaxCheckSum",
                table: "UserWallets");

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationCode",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Organizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NIN",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BVN",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
