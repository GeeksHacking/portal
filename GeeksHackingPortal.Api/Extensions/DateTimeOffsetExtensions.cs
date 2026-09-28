using GeeksHackingPortal.Api.Constants;

namespace GeeksHackingPortal.Api.Extensions;

public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Converts a (UTC) timestamp to <see cref="EventTimeZone"/> for presenting it to people.
    /// </summary>
    public static DateTimeOffset ToEventTime(this DateTimeOffset value)
    {
        return TimeZoneInfo.ConvertTime(value, EventTimeZone.Info);
    }

    /// <summary>
    /// OpenIddict persists its timestamps (e.g. <c>CreationDate</c>) as UTC <see cref="DateTime"/> values, but MySQL
    /// returns them with <see cref="DateTimeKind.Unspecified"/>, which would be serialized without an offset.
    /// Mark them as UTC so API responses carry an explicit offset like every other timestamp.
    /// </summary>
    public static DateTimeOffset AsUtcDateTimeOffset(this DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static DateTimeOffset? AsUtcDateTimeOffset(this DateTime? value)
    {
        return value?.AsUtcDateTimeOffset();
    }
}
