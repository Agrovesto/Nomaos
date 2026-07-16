using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationproducttable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "OrganizationProducts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaximumOrderQuantity",
                table: "OrganizationProducts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MinimumOrderQuantity",
                table: "OrganizationProducts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "OrganizationProducts");

            migrationBuilder.DropColumn(
                name: "MaximumOrderQuantity",
                table: "OrganizationProducts");

            migrationBuilder.DropColumn(
                name: "MinimumOrderQuantity",
                table: "OrganizationProducts");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
