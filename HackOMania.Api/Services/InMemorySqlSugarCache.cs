using System.Collections.Concurrent;
using SqlSugar;

namespace HackOMania.Api.Services;

public class InMemorySqlSugarCache : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    private class CacheItem
    {
        public object Value { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
    }

    public void Add<TV>(string key, TV value)
    {
        if (value != null)
        {
            _cache[key] = new CacheItem { Value = value };
        }
    }

    public void Add<TV>(string key, TV value, int cacheDurationInSeconds)
    {
        if (value != null)
        {
            _cache[key] = new CacheItem
            {
                Value = value,
                ExpiresAt = DateTime.UtcNow.AddSeconds(cacheDurationInSeconds)
            };
        }
    }

    public bool ContainsKey<TV>(string key)
    {
        if (!_cache.TryGetValue(key, out var item))
        {
            return false;
        }

        if (item.ExpiresAt.HasValue && item.ExpiresAt.Value < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    public TV Get<TV>(string key)
    {
        if (!_cache.TryGetValue(key, out var item))
        {
            return default!;
        }

        if (item.ExpiresAt.HasValue && item.ExpiresAt.Value < DateTime.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return default!;
        }

        return (TV)item.Value;
    }

    public IEnumerable<string> GetAllKey<TV>()
    {
        // Return all keys that match SqlSugar's pattern
        return _cache.Keys.Where(k => k.StartsWith("SqlSugarDataCache"));
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
        _cache.TryRemove(key, out _);
    }
}
