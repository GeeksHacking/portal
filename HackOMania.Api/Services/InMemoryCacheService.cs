using System.Collections.Concurrent;
using SqlSugar;

namespace HackOMania.Api.Services;

/// <summary>
/// In-memory cache service using ConcurrentDictionary.
/// Used as the primary cache for SqlSugar data caching.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public void Add<TV>(string key, TV value)
    {
        if (value != null)
        {
            _cache[key] = value;
        }
    }

    public void Add<TV>(string key, TV value, int cacheDurationInSeconds)
    {
        // TTL is not implemented because SqlSugar's auto-invalidation
        // (IsAutoRemoveDataCache = true) handles cache clearing on writes.
        if (value != null)
        {
            _cache[key] = value;
        }
    }

    public bool ContainsKey<TV>(string key)
    {
        return _cache.ContainsKey(key);
    }

    public TV Get<TV>(string key)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return (TV)value;
        }

        return default!;
    }

    public IEnumerable<string> GetAllKey<TV>()
    {
        // This cache is exclusively used by SqlSugar (DataInfoCacheService),
        // so all keys belong to SqlSugar. The TV type parameter is unused
        // by SqlSugar's internal cache invalidation logic.
        return _cache.Keys.ToList();
    }

    public TV GetOrCreate<TV>(
        string cacheKey,
        Func<TV> create,
        int cacheDurationInSeconds = int.MaxValue
    )
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return (TV)cached;
        }

        var result = create();
        if (result != null)
        {
            _cache[cacheKey] = result;
        }

        return result;
    }

    public void Remove<TV>(string key)
    {
        _cache.TryRemove(key, out _);
    }
}
