using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedadmincolumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WardId",
                table: "Organizations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminCode",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminUser",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminCodeSequences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCodeSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminInvitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_WardId",
                table: "Organizations",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AdminCode",
                table: "AspNetUsers",
                column: "AdminCode",
                unique: true,
                filter: "[AdminCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdminInvitations_TokenHash",
                table: "AdminInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminInvitations_UserId",
                table: "AdminInvitations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Wards_WardId",
                table: "Organizations",
                column: "WardId",
                principalTable: "Wards",
                principalColumn: "WardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Wards_WardId",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "AdminCodeSequences");

            migrationBuilder.DropTable(
                name: "AdminInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_WardId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AdminCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "AdminCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsAdminUser",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AspNetUsers");
        }
    }
}
