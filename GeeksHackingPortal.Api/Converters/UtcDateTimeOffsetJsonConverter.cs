using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeeksHackingPortal.Api.Converters;

/// <summary>
/// Writes every <see cref="DateTimeOffset"/> in API responses as UTC (e.g. <c>2026-03-07T01:00:00Z</c>), whatever
/// offset the value carries, so clients always receive one representation and decide how to present it.
/// Reading is unchanged: any ISO 8601 value with an offset is accepted.
/// </summary>
public sealed class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime);
    }
}
