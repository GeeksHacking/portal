using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SqlSugar;

namespace HackOMania.Api.Services;

public class SqlSugarDistributedCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web
    );
    private const int DefaultCacheSeconds = 60;
    private const string CacheKeyPrefix = "sqlsugar:data:";

    public void Add<TV>(string key, TV value)
    {
        Add(key, value, DefaultCacheSeconds);
    }

    public void Add<TV>(string key, TV value, int cacheDurationInSeconds)
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

    public bool ContainsKey<TV>(string key)
    {
        return cache.Get(GetCacheKey(key)) is not null;
    }

    public TV Get<TV>(string key)
    {
        var bytes = cache.Get(GetCacheKey(key));
        return bytes is null ? default! : JsonSerializer.Deserialize<TV>(bytes, SerializerOptions)!;
    }

    public IEnumerable<string> GetAllKey<TV>()
    {
        throw new NotSupportedException(
            $"{nameof(GetAllKey)} is not supported by IDistributedCache providers."
        );
    }

    public void Remove<TV>(string key)
    {
        cache.Remove(GetCacheKey(key));
    }

    public TV GetOrCreate<TV>(string cacheKey, Func<TV> create, int cacheDurationInSeconds)
    {
        if (ContainsKey<TV>(cacheKey))
        {
            return Get<TV>(cacheKey);
        }

        var value = create();
        Add(cacheKey, value, cacheDurationInSeconds);
        return value;
    }

    private static string GetCacheKey(string key) => $"{CacheKeyPrefix}{key}";
}
