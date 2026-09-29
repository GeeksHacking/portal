using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.DbMigrator;

/// <summary>
/// Drops the <c>TempDiff*</c> tables that SqlSugar schema diffs leave behind.
/// </summary>
/// <remarks>
/// <c>CodeFirst.GetDifferenceTables</c> creates a real <c>TempDiff{yyMMssHHmmssfff}</c> table for every entity and
/// only drops it once <c>InitTables</c> has completed. <c>CREATE TABLE</c> commits immediately on MySQL/TiDB, so a
/// failure after the table was created, or a run that is cancelled mid-diff, orphans the table. SqlSugar has no API
/// to clean these up. Only the migrator calls this: the deploy jobs share one concurrency group, so no other diff can
/// be using a temp table at the same time.
/// </remarks>
public static partial class SchemaDiffTempTableCleaner
{
    /// <summary>
    /// Drops leftover <c>TempDiff*</c> tables when <paramref name="allowDestructive"/> is set; otherwise only reports them.
    /// </summary>
    public static void CleanUp(ISqlSugarClient sql, ILogger logger, bool allowDestructive)
    {
        var tableNames = sql.DbMaintenance.GetTableInfoList(false)
            .Select(table => table.Name)
            .Where(name => TempDiffTableName().IsMatch(name))
            .ToArray();

        if (tableNames.Length == 0)
        {
            return;
        }

        if (!allowDestructive)
        {
            logger.LogWarning(
                "Found {TableCount} leftover SqlSugar schema diff table(s): {TableNames}. Run apply with --allow-destructive to drop them.",
                tableNames.Length,
                string.Join(", ", tableNames)
            );
            return;
        }

        logger.LogWarning(
            "Dropping {TableCount} leftover SqlSugar schema diff table(s).",
            tableNames.Length
        );

        foreach (var tableName in tableNames)
        {
            sql.DbMaintenance.DropTable(tableName);
            logger.LogInformation("Dropped leftover schema diff table {TableName}.", tableName);
        }
    }

    [GeneratedRegex(@"^TempDiff\d{15}$", RegexOptions.IgnoreCase)]
    private static partial Regex TempDiffTableName();
}
