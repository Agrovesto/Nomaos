using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedmembershiptable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @membershipSchema sysname = CASE
                    WHEN OBJECT_ID(N'[onboarding].[MemberShips]', N'U') IS NOT NULL THEN N'onboarding'
                    ELSE N'dbo'
                END;

                DECLARE @sql nvarchar(max) = N'
                ALTER TABLE [' + @membershipSchema + N'].[MemberShips]
                    ADD [RegistrationNumber] nvarchar(max) NULL;';

                EXEC sp_executesql @sql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @membershipSchema sysname = CASE
                    WHEN OBJECT_ID(N'[onboarding].[MemberShips]', N'U') IS NOT NULL THEN N'onboarding'
                    ELSE N'dbo'
                END;

                DECLARE @sql nvarchar(max) = N'
                ALTER TABLE [' + @membershipSchema + N'].[MemberShips]
                    DROP COLUMN [RegistrationNumber];';

                EXEC sp_executesql @sql;
                """);
        }
    }
}
