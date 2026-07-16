using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationfornullablelocationvalue : Migration
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

            migrationBuilder.AlterColumn<long>(
                name: "StateId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "LgaId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations");

            migrationBuilder.AlterColumn<long>(
                name: "StateId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "LgaId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations",
                column: "StateId",
                principalTable: "States",
                principalColumn: "StateId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
