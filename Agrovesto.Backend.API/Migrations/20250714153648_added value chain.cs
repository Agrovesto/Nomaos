using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedvaluechain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmerCategorysId = table.Column<long>(type: "bigint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "FarmerValueChainItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueChainTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerValueChainItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmerValueChainItems_ValueChainTypes_ValueChainTypeId",
                        column: x => x.ValueChainTypeId,
                        principalTable: "ValueChainTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmerValueChainItems_ValueChainTypeId",
                table: "FarmerValueChainItems",
                column: "ValueChainTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueChainTypes_FarmerCategorysId",
                table: "ValueChainTypes",
                column: "FarmerCategorysId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerValueChainItems");

            migrationBuilder.DropTable(
                name: "ValueChainTypes");

            migrationBuilder.DropTable(
                name: "FarmerCategorys");
        }
    }
}
