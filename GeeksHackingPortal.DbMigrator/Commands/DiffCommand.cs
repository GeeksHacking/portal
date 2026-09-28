using ConsoleAppFramework;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Services;
using GeeksHackingPortal.Api.Services.DataMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.DbMigrator.Commands;

public class DiffCommand(ILogger<DiffCommand> logger, ISqlSugarClient sql)
{
    /// <summary>
    /// Inspect pending SqlSugar schema differences and data migrations without applying changes.
    /// </summary>
    [Command("diff")]
    public Task<int> Diff(CancellationToken cancellationToken)
    {
        logger.LogInformation("Inspecting pending database schema differences.");
        cancellationToken.ThrowIfCancellationRequested();

        var report = SchemaDifferenceInspector.Inspect(sql);
        logger.LogInformation(
            "Collected SqlSugar schema differences for {EntityCount} entities.",
            report.EntityTypes.Count
        );

        SchemaDifferenceLogger.Write(logger, report);

        cancellationToken.ThrowIfCancellationRequested();
        var pendingDataMigrations = DataMigrationRunner.GetPending(sql);
        DataMigrationRunner.Preview(sql, logger, pendingDataMigrations);

        return Task.FromResult(report.HasDifferences || pendingDataMigrations.Count > 0 ? 2 : 0);
    }
}
