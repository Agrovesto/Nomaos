using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmertabletoincludecategorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmerValueChainItems_ValueChainTypes_ValueChainTypeId",
                table: "FarmerValueChainItems");

            migrationBuilder.DropTable(
                name: "ValueChainTypes");

            migrationBuilder.DropTable(
                name: "FarmerCategorys");

            migrationBuilder.RenameColumn(
                name: "ValueChainTypeId",
                table: "FarmerValueChainItems",
                newName: "FarmerId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "FarmerValueChainItems",
                newName: "ValueChainType");

            migrationBuilder.RenameIndex(
                name: "IX_FarmerValueChainItems_ValueChainTypeId",
                table: "FarmerValueChainItems",
                newName: "IX_FarmerValueChainItems_FarmerId");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "FarmerValueChainItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FarmerCategory",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerValueChainItems_Farmers_FarmerId",
                table: "FarmerValueChainItems",
                column: "FarmerId",
                principalTable: "Farmers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FarmerValueChainItems_Farmers_FarmerId",
                table: "FarmerValueChainItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "FarmerValueChainItems");

            migrationBuilder.DropColumn(
                name: "FarmerCategory",
                table: "Farmers");

            migrationBuilder.RenameColumn(
                name: "ValueChainType",
                table: "FarmerValueChainItems",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "FarmerId",
                table: "FarmerValueChainItems",
                newName: "ValueChainTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_FarmerValueChainItems_FarmerId",
                table: "FarmerValueChainItems",
                newName: "IX_FarmerValueChainItems_ValueChainTypeId");

            migrationBuilder.CreateTable(
                name: "FarmerCategorys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerCategory = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerCategorys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueChainTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmerCategorysId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueChainTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValueChainTypes_FarmerCategorys_FarmerCategorysId",
                        column: x => x.FarmerCategorysId,
                        principalTable: "FarmerCategorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValueChainTypes_FarmerCategorysId",
                table: "ValueChainTypes",
                column: "FarmerCategorysId");

            migrationBuilder.AddForeignKey(
                name: "FK_FarmerValueChainItems_ValueChainTypes_ValueChainTypeId",
                table: "FarmerValueChainItems",
                column: "ValueChainTypeId",
                principalTable: "ValueChainTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
