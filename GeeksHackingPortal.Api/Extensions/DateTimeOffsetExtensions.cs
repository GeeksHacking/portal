namespace GeeksHackingPortal.Api.Extensions;

public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// MySQL datetime columns do not preserve offsets. Backend-generated timestamps are written in UTC,
    /// so we reinterpret the stored clock time as UTC when shaping API responses.
    /// </summary>
    public static DateTimeOffset AssumeStoredAsUtc(this DateTimeOffset value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc));
    }

    public static DateTimeOffset? AssumeStoredAsUtc(this DateTimeOffset? value)
    {
        return value?.AssumeStoredAsUtc();
    }
}
