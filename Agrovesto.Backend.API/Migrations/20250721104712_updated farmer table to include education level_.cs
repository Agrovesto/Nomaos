using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedfarmertabletoincludeeducationlevel_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "YearsInAgric",
                table: "Farmers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "YearsInAgric",
                table: "Farmers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
