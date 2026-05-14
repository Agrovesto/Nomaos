using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updateinputpackagecategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItem_InputPackages_InputPackageId",
                table: "InputPackageItem");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_InputFinancingCategory_InputFinancingCategoryId",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_UserWallets_UserWalletId",
                table: "InputPackages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InputPackageItem",
                table: "InputPackageItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InputFinancingCategory",
                table: "InputFinancingCategory");

            migrationBuilder.RenameTable(
                name: "InputPackageItem",
                newName: "InputPackageItems");

            migrationBuilder.RenameTable(
                name: "InputFinancingCategory",
                newName: "InputFinancingCategories");

            migrationBuilder.RenameIndex(
                name: "IX_InputPackageItem_OrganizationProductId",
                table: "InputPackageItems",
                newName: "IX_InputPackageItems_OrganizationProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InputPackageItem_InputPackageId",
                table: "InputPackageItems",
                newName: "IX_InputPackageItems_InputPackageId");

            migrationBuilder.AlterColumn<long>(
                name: "UserWalletId",
                table: "InputPackages",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "InputFinancingCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedOn",
                table: "InputFinancingCategories",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddPrimaryKey(
                name: "PK_InputPackageItems",
                table: "InputPackageItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InputFinancingCategories",
                table: "InputFinancingCategories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItems_InputPackages_InputPackageId",
                table: "InputPackageItems",
                column: "InputPackageId",
                principalTable: "InputPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItems_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItems",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_InputFinancingCategories_InputFinancingCategoryId",
                table: "InputPackages",
                column: "InputFinancingCategoryId",
                principalTable: "InputFinancingCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_UserWallets_UserWalletId",
                table: "InputPackages",
                column: "UserWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItems_InputPackages_InputPackageId",
                table: "InputPackageItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackageItems_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_InputFinancingCategories_InputFinancingCategoryId",
                table: "InputPackages");

            migrationBuilder.DropForeignKey(
                name: "FK_InputPackages_UserWallets_UserWalletId",
                table: "InputPackages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InputPackageItems",
                table: "InputPackageItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InputFinancingCategories",
                table: "InputFinancingCategories");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "InputFinancingCategories");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "InputFinancingCategories");

            migrationBuilder.RenameTable(
                name: "InputPackageItems",
                newName: "InputPackageItem");

            migrationBuilder.RenameTable(
                name: "InputFinancingCategories",
                newName: "InputFinancingCategory");

            migrationBuilder.RenameIndex(
                name: "IX_InputPackageItems_OrganizationProductId",
                table: "InputPackageItem",
                newName: "IX_InputPackageItem_OrganizationProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InputPackageItems_InputPackageId",
                table: "InputPackageItem",
                newName: "IX_InputPackageItem_InputPackageId");

            migrationBuilder.AlterColumn<long>(
                name: "UserWalletId",
                table: "InputPackages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InputPackageItem",
                table: "InputPackageItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InputFinancingCategory",
                table: "InputFinancingCategory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItem_InputPackages_InputPackageId",
                table: "InputPackageItem",
                column: "InputPackageId",
                principalTable: "InputPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackageItem_OrganizationProducts_OrganizationProductId",
                table: "InputPackageItem",
                column: "OrganizationProductId",
                principalTable: "OrganizationProducts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_InputFinancingCategory_InputFinancingCategoryId",
                table: "InputPackages",
                column: "InputFinancingCategoryId",
                principalTable: "InputFinancingCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InputPackages_UserWallets_UserWalletId",
                table: "InputPackages",
                column: "UserWalletId",
                principalTable: "UserWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
