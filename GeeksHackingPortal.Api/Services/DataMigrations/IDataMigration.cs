using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services.DataMigrations;

/// <summary>
/// A one-off data change that is not idempotent and therefore must run exactly once per database.
/// </summary>
public interface IDataMigration
{
    /// <summary>
    /// Stable identifier recorded in <see cref="Entities.DataMigrationHistory"/>. Never change it once released.
    /// </summary>
    string Id { get; }

    string Description { get; }

    /// <summary>
    /// Logs what <see cref="Apply"/> would change without modifying any data.
    /// </summary>
    void Preview(ISqlSugarClient sql, ILogger logger);

    /// <summary>
    /// Applies the change. Runs inside a transaction together with the history record.
    /// </summary>
    void Apply(ISqlSugarClient sql, ILogger logger);
}
