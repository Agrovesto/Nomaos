using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class updatedforgotandresetpasswordflow : Migration
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
                CREATE TABLE [dbo].[CredentialResetAttempts] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] nvarchar(450) NOT NULL,
                    [PurposeKey] nvarchar(50) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_CredentialResetAttempts] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CredentialResetAttempts_AspNetUsers_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [' + @userSchema + N'].[AspNetUsers] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_CredentialResetAttempts_UserId_PurposeKey_CreatedAt]
                    ON [dbo].[CredentialResetAttempts] ([UserId], [PurposeKey], [CreatedAt]);';

                EXEC sp_executesql @sql;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialResetAttempts");
        }
    }
}
