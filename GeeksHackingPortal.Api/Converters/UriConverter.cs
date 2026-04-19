using System.Data;
using SqlSugar;

namespace GeeksHackingPortal.Api.Converters;

public class UriConverter : ISugarDataConverter
{
    public SugarParameter ParameterConverter<T>(object value, int index)
    {
        var name = "@UriParam" + index;

        if (value is Uri uri)
        {
            return new SugarParameter(name, uri.ToString());
        }

        return new SugarParameter(name, value.ToString());
    }

    public T QueryConverter<T>(IDataRecord dataRecord, int index)
    {
        var value = dataRecord.GetValue(index);

        if (value is null or DBNull)
        {
            return default!;
        }

        var stringValue = value.ToString();

        if (string.IsNullOrEmpty(stringValue))
        {
            return default!;
        }

        if (typeof(T) == typeof(Uri))
        {
            return (T)(object)new Uri(stringValue);
        }

        return (T)value;
    }
}
