using ConsoleAppFramework;
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
        try
        {
            logger.LogInformation("Inspecting pending database schema differences.");
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Creating database connection.");
            var entityTypes = SchemaDiffLogger.GetEntityTypes();
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Collecting SqlSugar schema differences for {EntityCount} entities.", entityTypes.Length);
            var differenceProvider = sql.CodeFirst.GetDifferenceTables(entityTypes);
            var schemaDifferences = differenceProvider.ToDiffList()
                .Where(table => table.IsDiff)
                .ToArray();

            SchemaDiffLogger.Write(
                logger,
                differenceProvider.ToDiffString()?.Trim() ?? string.Empty,
                schemaDifferences
            );

            return Task.FromResult(schemaDifferences.Length > 0 ? 2 : 0);
        }
        catch (Exception exception)
        {
            return Task.FromException<int>(exception);
        }
    }
}
