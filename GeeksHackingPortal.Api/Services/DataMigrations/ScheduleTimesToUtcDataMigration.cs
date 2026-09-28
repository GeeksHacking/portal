using GeeksHackingPortal.Api.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services.DataMigrations;

/// <summary>
/// Converts organizer-entered schedule times from Singapore wall-clock time to UTC.
/// </summary>
/// <remarks>
/// Before <see cref="Data.SqlSugarClientFactory"/> normalized every <see cref="DateTimeOffset"/> to UTC, SqlSugar
/// stored the wall-clock part of whatever offset it was given. System timestamps (<see cref="DateTimeOffset.UtcNow"/>)
/// were therefore stored in UTC, while schedule fields were stored in Singapore time because the web app submits
/// them with an explicit <c>+08:00</c> offset and the API host runs with <c>TZ=Asia/Singapore</c>, which made
/// them read back correctly. Only these schedule columns are shifted; everything else already holds UTC.
/// </remarks>
public sealed class ScheduleTimesToUtcDataMigration : IDataMigration
{
    // Singapore has observed UTC+8 without daylight saving time since 1982.
    private const int SingaporeUtcOffsetHours = 8;

    // SqlSugar writes default(DateTimeOffset) as its MySQL minimum date. Those "unset" values are left untouched.
    private static readonly DateTime UnsetValue = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly (Type EntityType, string PropertyName)[] ScheduleColumns =
    [
        (typeof(Activity), nameof(Activity.StartTime)),
        (typeof(Activity), nameof(Activity.EndTime)),
        (typeof(Hackathon), nameof(Hackathon.LegacyEventStartDate)),
        (typeof(Hackathon), nameof(Hackathon.LegacyEventEndDate)),
        (typeof(Hackathon), nameof(Hackathon.SubmissionsStartDate)),
        (typeof(Hackathon), nameof(Hackathon.ChallengeSelectionEndDate)),
        (typeof(Hackathon), nameof(Hackathon.SubmissionsEndDate)),
        (typeof(Hackathon), nameof(Hackathon.JudgingStartDate)),
        (typeof(Hackathon), nameof(Hackathon.JudgingEndDate)),
        (typeof(Workshop), nameof(Workshop.LegacyStartTime)),
        (typeof(Workshop), nameof(Workshop.LegacyEndTime)),
        (typeof(EventTimelineItem), nameof(EventTimelineItem.StartTime)),
        (typeof(EventTimelineItem), nameof(EventTimelineItem.EndTime)),
    ];

    public string Id => "20260928-schedule-times-to-utc";

    public string Description =>
        $"Convert organizer-entered schedule times stored as Singapore wall-clock time (UTC+{SingaporeUtcOffsetHours}) to UTC.";

    public void Preview(ISqlSugarClient sql, ILogger logger)
    {
        foreach (var (table, column) in GetExistingColumns(sql))
        {
            var count = sql.Ado.GetInt(
                $"SELECT COUNT(*) FROM `{table}` WHERE `{column}` > @unsetValue",
                new SugarParameter("@unsetValue", UnsetValue)
            );
            logger.LogInformation("  {Table}.{Column}: {RowCount} row(s) will be shifted by -{Hours}h.", table, column, count, SingaporeUtcOffsetHours);
        }

        if (!sql.DbMaintenance.IsAnyTable(nameof(Activity), false))
        {
            return;
        }

        var activities = sql.Queryable<Activity>()
            .OrderBy(activity => activity.StartTime)
            .Select(activity => new
            {
                activity.Id,
                activity.Kind,
                activity.Title,
                activity.StartTime,
                activity.EndTime,
            })
            .ToList();

        foreach (var activity in activities)
        {
            logger.LogInformation(
                "  {Kind} {ActivityId} \"{Title}\": {StartTime:yyyy-MM-dd HH:mm} - {EndTime:yyyy-MM-dd HH:mm} SGT will be stored as {UtcStartTime:yyyy-MM-dd HH:mm} - {UtcEndTime:yyyy-MM-dd HH:mm} UTC",
                activity.Kind,
                activity.Id,
                activity.Title,
                activity.StartTime.DateTime,
                activity.EndTime.DateTime,
                activity.StartTime.DateTime.AddHours(-SingaporeUtcOffsetHours),
                activity.EndTime.DateTime.AddHours(-SingaporeUtcOffsetHours)
            );
        }
    }

    public void Apply(ISqlSugarClient sql, ILogger logger)
    {
        foreach (var (table, column) in GetExistingColumns(sql))
        {
            var affected = sql.Ado.ExecuteCommand(
                $"UPDATE `{table}` SET `{column}` = DATE_SUB(`{column}`, INTERVAL {SingaporeUtcOffsetHours} HOUR) WHERE `{column}` > @unsetValue",
                new SugarParameter("@unsetValue", UnsetValue)
            );
            logger.LogInformation("Converted {RowCount} value(s) in {Table}.{Column} to UTC.", affected, table, column);
        }
    }

    private static IEnumerable<(string Table, string Column)> GetExistingColumns(ISqlSugarClient sql)
    {
        foreach (var (entityType, propertyName) in ScheduleColumns)
        {
            var entity = sql.EntityMaintenance.GetEntityInfo(entityType);
            if (!sql.DbMaintenance.IsAnyTable(entity.DbTableName, false))
            {
                continue;
            }

            var column = entity.Columns.Single(column => column.PropertyName == propertyName);
            yield return (entity.DbTableName, column.DbColumnName);
        }
    }
}
