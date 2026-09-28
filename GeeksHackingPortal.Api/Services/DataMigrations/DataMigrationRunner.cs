using GeeksHackingPortal.Api.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services.DataMigrations;

public static class DataMigrationRunner
{
    /// <summary>
    /// All data migrations in the order they must be applied.
    /// </summary>
    public static IReadOnlyList<IDataMigration> Migrations { get; } = [new ScheduleTimesToUtcDataMigration()];

    public static IReadOnlyList<IDataMigration> GetPending(ISqlSugarClient sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        if (!sql.DbMaintenance.IsAnyTable(nameof(DataMigrationHistory), false))
        {
            return Migrations;
        }

        var applied = sql.Queryable<DataMigrationHistory>()
            .Select(migration => migration.Id)
            .ToList()
            .ToHashSet(StringComparer.Ordinal);

        return Migrations.Where(migration => !applied.Contains(migration.Id)).ToArray();
    }

    public static void Preview(ISqlSugarClient sql, ILogger logger, IReadOnlyList<IDataMigration> pending)
    {
        if (pending.Count == 0)
        {
            logger.LogInformation("No pending data migrations.");
            return;
        }

        logger.LogInformation("Pending data migrations detected: {MigrationCount}.", pending.Count);

        foreach (var migration in pending)
        {
            logger.LogInformation("{MigrationId}: {Description}", migration.Id, migration.Description);
            migration.Preview(sql, logger);
        }
    }

    public static void ApplyPending(ISqlSugarClient sql, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = GetPending(sql);
        if (pending.Count == 0)
        {
            logger.LogInformation("No pending data migrations.");
            return;
        }

        foreach (var migration in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation(
                "Applying data migration {MigrationId}: {Description}",
                migration.Id,
                migration.Description
            );

            var result = sql.Ado.UseTran(() =>
            {
                migration.Apply(sql, logger);
                sql.Insertable(
                        new DataMigrationHistory { Id = migration.Id, AppliedAt = DateTimeOffset.UtcNow }
                    )
                    .ExecuteCommand();
            });

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Data migration {migration.Id} failed and was rolled back.",
                    result.ErrorException
                );
            }

            logger.LogInformation("Data migration {MigrationId} was applied.", migration.Id);
        }
    }
}
