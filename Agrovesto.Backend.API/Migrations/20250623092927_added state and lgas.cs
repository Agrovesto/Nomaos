using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedstateandlgas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StatesId",
                table: "LGAs");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_LGAs_LGAId",
                table: "Wards");

            migrationBuilder.RenameColumn(
                name: "LGAId",
                table: "Wards",
                newName: "LgaId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Wards",
                newName: "WardId");

            migrationBuilder.RenameIndex(
                name: "IX_Wards_LGAId",
                table: "Wards",
                newName: "IX_Wards_LgaId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "States",
                newName: "StateId");

            migrationBuilder.RenameColumn(
                name: "StatesId",
                table: "LGAs",
                newName: "StatesStateId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LGAs",
                newName: "LgaId");

            migrationBuilder.RenameIndex(
                name: "IX_LGAs_StatesId",
                table: "LGAs",
                newName: "IX_LGAs_StatesStateId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LGAs_States_StatesStateId",
                table: "LGAs");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_LGAs_LgaId",
                table: "Wards");

            migrationBuilder.RenameColumn(
                name: "LgaId",
                table: "Wards",
                newName: "LGAId");

            migrationBuilder.RenameColumn(
                name: "WardId",
                table: "Wards",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Wards_LgaId",
                table: "Wards",
                newName: "IX_Wards_LGAId");

            migrationBuilder.RenameColumn(
                name: "StateId",
                table: "States",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "StatesStateId",
                table: "LGAs",
                newName: "StatesId");

            migrationBuilder.RenameColumn(
                name: "LgaId",
                table: "LGAs",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_LGAs_StatesStateId",
                table: "LGAs",
                newName: "IX_LGAs_StatesId");

            migrationBuilder.AddForeignKey(
                name: "FK_LGAs_States_StatesId",
                table: "LGAs",
                column: "StatesId",
                principalTable: "States",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_LGAs_LGAId",
                table: "Wards",
                column: "LGAId",
                principalTable: "LGAs",
                principalColumn: "Id");
        }
    }
}
