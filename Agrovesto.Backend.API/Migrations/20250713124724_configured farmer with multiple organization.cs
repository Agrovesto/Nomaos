using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class configuredfarmerwithmultipleorganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganizationCode",
                table: "AspNetUsers",
                newName: "NIN");

            migrationBuilder.AddColumn<string>(
                name: "BVN",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserOrganizations",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizations", x => new { x.UserId, x.OrganizationCode });
                    table.ForeignKey(
                        name: "FK_UserOrganizations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "BVN",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "NIN",
                table: "AspNetUsers",
                newName: "OrganizationCode");
        }
    }
}
