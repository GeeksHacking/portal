using System.Text.Json;
using SqlSugar;
using StackExchange.Redis;

namespace HackOMania.Api.Services;

public class SqlSugarRedisCache(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    public void Add<TV>(string key, TV value)
    {
        if (value != null)
        {
            var json = JsonSerializer.Serialize(value);
            _db.StringSet(key, json);
        }
    }

    public void Add<TV>(string key, TV value, int cacheDurationInSeconds)
    {
        if (value != null)
        {
            var json = JsonSerializer.Serialize(value);
            _db.StringSet(key, json, TimeSpan.FromSeconds(cacheDurationInSeconds));
        }
    }

    public bool ContainsKey<TV>(string key)
    {
        return _db.KeyExists(key);
    }

    public TV Get<TV>(string key)
    {
        var value = _db.StringGet(key);
        if (value.IsNullOrEmpty)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<TV>(value.ToString())!;
    }

    public IEnumerable<string> GetAllKey<TV>()
    {
        // Only query keys used by SqlSugar for performance
        var server = connectionMultiplexer.GetServers().FirstOrDefault();
        if (server == null)
        {
            return [];
        }

        return server.Keys(pattern: "SqlSugarDataCache.*").Select(k => k.ToString());
    }

    public TV GetOrCreate<TV>(
        string cacheKey,
        Func<TV> create,
        int cacheDurationInSeconds = int.MaxValue
    )
    {
        if (ContainsKey<TV>(cacheKey))
        {
            var result = Get<TV>(cacheKey);
            if (result == null)
            {
                return create();
            }

            return result;
        }
        else
        {
            var result = create();
            Add(cacheKey, result, cacheDurationInSeconds);
            return result;
        }
    }

    public void Remove<TV>(string key)
    {
        _db.KeyDelete(key);
    }
}
