using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261006141000_create_service_schemas")]
public partial class createserviceschemas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "onboarding");
        migrationBuilder.EnsureSchema(name: "wallet");
        migrationBuilder.EnsureSchema(name: "input_financing");
        migrationBuilder.EnsureSchema(name: "monitoring_evaluation");
        migrationBuilder.EnsureSchema(name: "warehouse");
        migrationBuilder.EnsureSchema(name: "service_provider");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropSchemaIfEmpty(migrationBuilder, "monitoring_evaluation");
        DropSchemaIfEmpty(migrationBuilder, "input_financing");
        DropSchemaIfEmpty(migrationBuilder, "service_provider");
        DropSchemaIfEmpty(migrationBuilder, "warehouse");
        DropSchemaIfEmpty(migrationBuilder, "wallet");
        DropSchemaIfEmpty(migrationBuilder, "onboarding");
    }

    private static void DropSchemaIfEmpty(MigrationBuilder migrationBuilder, string schema)
    {
        migrationBuilder.Sql($"""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.objects
                WHERE schema_id = SCHEMA_ID(N'{schema}')
            )
            AND SCHEMA_ID(N'{schema}') IS NOT NULL
            BEGIN
                EXEC(N'DROP SCHEMA [{schema}]');
            END
            """);
    }
}
