using SqlSugar;

namespace HackOMania.Api.Services;

/// <summary>
/// No-operation cache service that performs no caching.
/// Used as a fallback when Redis is not available during startup.
/// </summary>
public class NoOpCacheService : ICacheService
{
    public void Add<TV>(string key, TV value)
    {
        // No-op: do nothing
    }

    public void Add<TV>(string key, TV value, int cacheDurationInSeconds)
    {
        // No-op: do nothing
    }

    public bool ContainsKey<TV>(string key)
    {
        // Always return false - no cache
        return false;
    }

    public TV Get<TV>(string key)
    {
        // Always return default - no cached value
        return default!;
    }

    public IEnumerable<string> GetAllKey<TV>()
    {
        // Return empty - no keys
        return [];
    }

    public TV GetOrCreate<TV>(string cacheKey, Func<TV> create, int cacheDurationInSeconds = int.MaxValue)
    {
        // Always create - no caching
        return create();
    }

    public void Remove<TV>(string key)
    {
        // No-op: do nothing
    }
}
