using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedflutterwaveservice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentProviders",
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
                    table.PrimaryKey("PK_PaymentProviders", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderParameters",
                columns: table => new
                {
                    ParameterId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    //ProviderId = table.Column<long>(type: "bigint", nullable: false),
                   PaymentProviderProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Env = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderParameters", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_PaymentProviderParameters_PaymentProviders_PaymentProviderProviderId",
                        column: x => x.PaymentProviderProviderId,
                        principalTable: "PaymentProviders",
                        principalColumn: "ProviderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderParameters_PaymentProviderProviderId",
                table: "PaymentProviderParameters",
                column: "PaymentProviderProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentProviderParameters");

            migrationBuilder.DropTable(
                name: "PaymentProviders");
        }
    }
}
