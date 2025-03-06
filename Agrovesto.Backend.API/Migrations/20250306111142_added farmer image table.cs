using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedfarmerimagetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmerImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmerUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IMageKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BelongToBusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmerImages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmerImages");
        }
    }
}
