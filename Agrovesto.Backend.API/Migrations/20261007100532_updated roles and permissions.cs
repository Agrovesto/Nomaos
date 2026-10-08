using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedrolesandpermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminSession",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "BusinessId",
                table: "AspNetUserRoles",
                type: "bigint",
                nullable: true,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUserRoles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(@"
            UPDATE ur
            SET ur.BusinessId = x.BusinessId
            FROM AspNetUserRoles ur
            INNER JOIN (
                SELECT ub.UserId,
                       COALESCE(MIN(CASE WHEN ub.IsDefault = 1 THEN ub.BusinessId END),
                                MIN(ub.BusinessId)) AS BusinessId
                FROM UserBusinesses ub
                WHERE ub.IsActive = 1
                GROUP BY ub.UserId
            ) x ON x.UserId = ur.UserId;");

            migrationBuilder.AlterColumn<long>(
           name: "BusinessId", table: "AspNetUserRoles",
           type: "bigint", 
           nullable: true, 
           oldClrType: typeof(long), 
           oldType: "bigint", oldNullable: true);

            migrationBuilder.Sql(@"
             INSERT INTO AspNetUserRoles (UserId, RoleId, BusinessId, IsActive)
            SELECT ur.UserId, ur.RoleId, ub.BusinessId, 1
            FROM AspNetUserRoles ur
             JOIN UserBusinesses ub
                 ON ub.UserId = ur.UserId AND ub.IsActive = 1 AND ub.BusinessId <> ur.BusinessId
            WHERE NOT EXISTS (
                 SELECT 1 FROM AspNetUserRoles x
                 WHERE x.UserId = ur.UserId AND x.RoleId = ur.RoleId AND x.BusinessId = ub.BusinessId);
");

            // Ensure BusinessId values reference an existing Business before adding PK/FK.
            // If no Business exists, create a migration placeholder. Then set any NULL or invalid
            // BusinessId on AspNetUserRoles to the smallest existing BusinessId.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Businesses)
BEGIN
    INSERT INTO Businesses (BusinessCode, BusinessCodeSignage, BusinessCodeSignageSalt, BusinessName, ContactEmail, BusinessEmail, PhoneNumber, Address, BusinessStatus, Category, KycCategory, BusinessLogo, CreatedOn, IsActive)
    VALUES ('__MIGRATION_DEFAULT__','__MIG_SIGN__','__MIG_SALT__','Migration Default','migration@local','migration@local','0000000000','Migration address','Active','','','', GETDATE(), 1);
END

DECLARE @fallback bigint = (SELECT MIN(BusinessId) FROM Businesses);

UPDATE AspNetUserRoles
SET BusinessId = @fallback
WHERE BusinessId IS NULL OR BusinessId NOT IN (SELECT BusinessId FROM Businesses);

ALTER TABLE AspNetUserRoles ALTER COLUMN BusinessId bigint NOT NULL;");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId", "BusinessId" });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_BusinessId",
                table: "AspNetUserRoles",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId_BusinessId_IsActive",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Businesses_BusinessId",
                table: "AspNetUserRoles",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
            DELETE ur FROM AspNetUserRoles ur
            JOIN AspNetUserRoles keep
              ON keep.UserId = ur.UserId AND keep.RoleId = ur.RoleId AND keep.BusinessId < ur.BusinessId;");

            migrationBuilder.DropForeignKey("FK_AspNetUserRoles_Businesses_BusinessId", "AspNetUserRoles");
            migrationBuilder.DropPrimaryKey("PK_AspNetUserRoles", "AspNetUserRoles");
            migrationBuilder.AddPrimaryKey("PK_AspNetUserRoles", "AspNetUserRoles", new[] { "UserId", "RoleId" });
            migrationBuilder.DropIndex("IX_AspNetUserRoles_UserId_BusinessId_IsActive", "AspNetUserRoles");
            migrationBuilder.DropColumn("BusinessId", "AspNetUserRoles");
            migrationBuilder.DropColumn("IsActive", "AspNetUserRoles");



            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Businesses_BusinessId",
                table: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_BusinessId",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_UserId_BusinessId_IsActive",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "IsAdminSession",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUserRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });
        }
    }
}
