using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261006142000_move_monitoring_evaluation_and_add_outbox_metadata")]
public partial class movemonitoringevaluationandaddoutboxmetadata : Migration
{
    private const string MonitoringEvaluationSchema = "monitoring_evaluation";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(MonitoringEvaluationSchema);

        foreach (var table in MonitoringEvaluationTables)
        {
            migrationBuilder.RenameTable(
                name: table,
                newName: table,
                newSchema: MonitoringEvaluationSchema);
        }

        migrationBuilder.AddColumn<int>(
            name: "Attempts",
            table: "OutboxMessages",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "LastError",
            table: "OutboxMessages",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "OutboxMessages",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Source_IsProcessed_CreatedOn",
            table: "OutboxMessages",
            columns: new[] { "Source", "IsProcessed", "CreatedOn" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_Source_IsProcessed_CreatedOn",
            table: "OutboxMessages");

        migrationBuilder.DropColumn(name: "Attempts", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "LastError", table: "OutboxMessages");
        migrationBuilder.DropColumn(name: "Source", table: "OutboxMessages");

        foreach (var table in MonitoringEvaluationTables)
        {
            migrationBuilder.RenameTable(
                name: table,
                schema: MonitoringEvaluationSchema,
                newName: table);
        }
    }

    private static readonly string[] MonitoringEvaluationTables =
    [
        "MandEActivityTypes",
        "MandESubActivities",
        "MandEFieldDefinitions",
        "MandEFarmActivities",
        "MandEDatasets",
        "MandEDataFieldDefinitions",
        "MandERecords",
        "MandEChartDefinitions"
    ];
}
