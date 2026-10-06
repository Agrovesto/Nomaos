using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updateduserstablewitfmandlastname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @userSchema sysname = CASE
                    WHEN OBJECT_ID(N'[onboarding].[AspNetUsers]', N'U') IS NOT NULL THEN N'onboarding'
                    ELSE N'dbo'
                END;

                DECLARE @sql nvarchar(max) = N'
                ALTER TABLE [' + @userSchema + N'].[AspNetUsers] ADD
                    [FirstName] nvarchar(max) NULL,
                    [LastName] nvarchar(max) NULL,
                    [MiddleName] nvarchar(max) NULL;';

                EXEC sp_executesql @sql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @userSchema sysname = CASE
                    WHEN OBJECT_ID(N'[onboarding].[AspNetUsers]', N'U') IS NOT NULL THEN N'onboarding'
                    ELSE N'dbo'
                END;

                DECLARE @sql nvarchar(max) = N'
                ALTER TABLE [' + @userSchema + N'].[AspNetUsers] DROP COLUMN
                    [FirstName], [LastName], [MiddleName];';

                EXEC sp_executesql @sql;
                """);
        }
    }
}
