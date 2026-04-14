using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedrepaymentstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmerUserId",
                table: "Repayments");

            migrationBuilder.AddColumn<long>(
                name: "FarmerId",
                table: "Repayments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmerId",
                table: "Repayments");

            migrationBuilder.AddColumn<string>(
                name: "FarmerUserId",
                table: "Repayments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
