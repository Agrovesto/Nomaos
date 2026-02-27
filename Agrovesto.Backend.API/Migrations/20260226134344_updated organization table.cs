using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedorganizationtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalAreaInSqMetre",
                table: "Farms",
                newName: "FarmSizeInSqMetre");

            migrationBuilder.RenameColumn(
                name: "TotalAreaInDegress",
                table: "Farms",
                newName: "FarmSizeInDegress");

            migrationBuilder.RenameColumn(
                name: "AreaSize",
                table: "Farms",
                newName: "FarmSize");

            migrationBuilder.RenameColumn(
                name: "OrganizationName",
                table: "BusinessOrganizations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "BusinessOrganizations",
                newName: "RoleInOrganization");

            migrationBuilder.RenameColumn(
                name: "Contact",
                table: "BusinessOrganizations",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "ContacPerson",
                table: "BusinessOrganizations",
                newName: "OrganizationPhone");

            migrationBuilder.AddColumn<string>(
                name: "FarmSizeType",
                table: "Farms",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CACNumber",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyType",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "BusinessOrganizations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "LgaId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardedOn",
                table: "BusinessOrganizations",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "StateId",
                table: "BusinessOrganizations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOrganizations_LgaId",
                table: "BusinessOrganizations",
                column: "LgaId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOrganizations_StateId",
                table: "BusinessOrganizations",
                column: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations",
                column: "LgaId",
                principalTable: "LGAs",
                principalColumn: "LgaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations",
                column: "StateId",
                principalTable: "States",
                principalColumn: "StateId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_LGAs_LgaId",
                table: "BusinessOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessOrganizations_States_StateId",
                table: "BusinessOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessOrganizations_LgaId",
                table: "BusinessOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessOrganizations_StateId",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "FarmSizeType",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "CACNumber",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "CompanyType",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "LgaId",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "OnboardedOn",
                table: "BusinessOrganizations");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "BusinessOrganizations");

            migrationBuilder.RenameColumn(
                name: "FarmSizeInSqMetre",
                table: "Farms",
                newName: "TotalAreaInSqMetre");

            migrationBuilder.RenameColumn(
                name: "FarmSizeInDegress",
                table: "Farms",
                newName: "TotalAreaInDegress");

            migrationBuilder.RenameColumn(
                name: "FarmSize",
                table: "Farms",
                newName: "AreaSize");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "BusinessOrganizations",
                newName: "OrganizationName");

            migrationBuilder.RenameColumn(
                name: "RoleInOrganization",
                table: "BusinessOrganizations",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "BusinessOrganizations",
                newName: "Contact");

            migrationBuilder.RenameColumn(
                name: "OrganizationPhone",
                table: "BusinessOrganizations",
                newName: "ContacPerson");
        }
    }
}
