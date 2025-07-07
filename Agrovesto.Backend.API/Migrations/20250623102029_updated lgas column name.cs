using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedlgascolumnname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StatesStateId",
                table: "LGAs");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_LGAs_LgaId",
                table: "Wards");

            migrationBuilder.AlterColumn<long>(
                name: "LgaId",
                table: "Wards",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "StatesStateId",
                table: "LGAs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LGAs_States_StatesStateId",
                table: "LGAs",
                column: "StatesStateId",
                principalTable: "States",
                principalColumn: "StateId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_LGAs_LgaId",
                table: "Wards",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StatesStateId",
                table: "LGAs");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_LGAs_LgaId",
                table: "Wards");

            migrationBuilder.AlterColumn<long>(
                name: "LgaId",
                table: "Wards",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "StatesStateId",
                table: "LGAs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_LGAs_States_StatesStateId",
                table: "LGAs",
                column: "StatesStateId",
                principalTable: "States",
                principalColumn: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_LGAs_LgaId",
                table: "Wards",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId");
        }
    }
}
