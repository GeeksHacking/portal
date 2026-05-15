using ConsoleAppFramework;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.DbMigrator.Commands;

public class DiffCommand(ILogger<DiffCommand> logger, ISqlSugarClient sql)
{
    /// <summary>
    /// Inspect pending SqlSugar schema differences without applying changes.
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

        return Task.FromResult(report.HasDifferences ? 2 : 0);
    }
}
