using System.Data;
using SqlSugar;

namespace HackOMania.Api.Converters;

public class DateTimeOffsetUtcConverter : ISugarDataConverter
{
    public SugarParameter ParameterConverter<T>(object value, int index)
    {
        var name = "@DateTimeOffsetParam" + index;

        if (value is DateTimeOffset dateTimeOffset)
        {
            return new SugarParameter(name, dateTimeOffset.UtcDateTime);
        }

        if (value is DateTime dateTime)
        {
            var utcDateTime =
                dateTime.Kind == DateTimeKind.Utc
                    ? dateTime
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            return new SugarParameter(name, utcDateTime);
        }

        return new SugarParameter(name, value);
    }

    public T QueryConverter<T>(IDataRecord dataRecord, int index)
    {
        var value = dataRecord.GetValue(index);

        if (value is null or DBNull)
        {
            return default!;
        }

        var nullableType = Nullable.GetUnderlyingType(typeof(T));
        var targetType = nullableType ?? typeof(T);

        if (targetType != typeof(DateTimeOffset))
        {
            return (T)value;
        }

        DateTimeOffset result;

        if (value is DateTimeOffset dateTimeOffset)
        {
            result = dateTimeOffset.ToUniversalTime();
        }
        else if (value is DateTime dateTime)
        {
            var utcDateTime =
                dateTime.Kind == DateTimeKind.Utc
                    ? dateTime
                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            result = new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        }
        else if (!DateTimeOffset.TryParse(value.ToString(), out result))
        {
            return default!;
        }
        else
        {
            result = result.ToUniversalTime();
        }

        if (nullableType is not null)
        {
            return (T)(object)(DateTimeOffset?)result;
        }

        return (T)(object)result;
    }
}
