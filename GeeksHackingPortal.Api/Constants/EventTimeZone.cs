namespace GeeksHackingPortal.Api.Constants;

/// <summary>
/// The time zone events are held in. Timestamps are stored and returned in UTC; this is only used when the
/// server itself has to present a date to people, such as in emails.
/// </summary>
public static class EventTimeZone
{
    public const string Id = "Asia/Singapore";

    public static TimeZoneInfo Info { get; } = Resolve();

    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(Id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Hosts without time zone data. Singapore has observed UTC+8 without daylight saving time since 1982.
            return TimeZoneInfo.CreateCustomTimeZone(Id, TimeSpan.FromHours(8), Id, Id);
        }
    }
}
