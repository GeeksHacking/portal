using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SqlSugar;

namespace HackOMania.Api.Services;

public class SqlSugarDistributedCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const int DefaultCacheSeconds = 60;
    private const string CacheKeyPrefix = "sqlsugar:data:";

    public void Add<V>(string key, V value)
    {
        Add(key, value, DefaultCacheSeconds);
    }

    public void Add<V>(string key, V value, int cacheDurationInSeconds)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        cache.Set(
            GetCacheKey(key),
            bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(
                    Math.Max(1, cacheDurationInSeconds)
                ),
            }
        );
    }

    public bool ContainsKey<V>(string key)
    {
        return cache.Get(GetCacheKey(key)) is not null;
    }

    public V Get<V>(string key)
    {
        var bytes = cache.Get(GetCacheKey(key));
        if (bytes is null)
        {
            return default!;
        }

        return JsonSerializer.Deserialize<V>(bytes, SerializerOptions)!;
    }

    public IEnumerable<string> GetAllKey<V>()
    {
        return [];
    }

    public void Remove<V>(string key)
    {
        cache.Remove(GetCacheKey(key));
    }

    public V GetOrCreate<V>(string cacheKey, Func<V> create, int cacheDurationInSeconds)
    {
        if (ContainsKey<V>(cacheKey))
        {
            return Get<V>(cacheKey);
        }

        var value = create();
        Add(cacheKey, value, cacheDurationInSeconds);
        return value;
    }

    private static string GetCacheKey(string key) => $"{CacheKeyPrefix}{key}";
}
