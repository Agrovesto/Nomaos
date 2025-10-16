using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class removedtargetgroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchemeTarget");

            migrationBuilder.RenameColumn(
                name: "ConfigurationCode",
                table: "InputFinancingSchemes",
                newName: "InputFinancingConfigurationCode");

            migrationBuilder.AlterColumn<long>(
                name: "WarehouseId",
                table: "InputFinancingSchemes",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InputFinancingConfigurationCode",
                table: "InputFinancingSchemes",
                newName: "ConfigurationCode");

            migrationBuilder.AlterColumn<string>(
                name: "WarehouseId",
                table: "InputFinancingSchemes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "SchemeTarget",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputFinancingSchemeId = table.Column<long>(type: "bigint", nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemeTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemeTarget_InputFinancingSchemes_InputFinancingSchemeId",
                        column: x => x.InputFinancingSchemeId,
                        principalTable: "InputFinancingSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchemeTarget_InputFinancingSchemeId",
                table: "SchemeTarget",
                column: "InputFinancingSchemeId");
        }
    }
}
