using SqlSugar;

namespace GeeksHackingPortal.Api.Entities;

/// <summary>
/// Records one-off data migrations applied by the database migrator so that they never run twice.
/// </summary>
public class DataMigrationHistory
{
    [SugarColumn(IsPrimaryKey = true, Length = 128)]
    public string Id { get; set; } = null!;

    public DateTimeOffset AppliedAt { get; set; }
}
