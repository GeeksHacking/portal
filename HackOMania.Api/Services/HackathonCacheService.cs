using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace HackOMania.Api.Services;

public class HackathonCacheService(IDistributedCache cache) : IHackathonCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web
    );
    private const string CacheKeyPrefix = "hackathon:";
    private const string ParticipantListKey = $"{CacheKeyPrefix}participants:list";
    private const string OrganizerListKeyPrefix = $"{CacheKeyPrefix}organizers:list:";
    private const string CacheVersionKey = $"{CacheKeyPrefix}version";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(GetCacheKey(key), ct);
        if (bytes is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default
    )
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration,
        };
        await cache.SetAsync(GetCacheKey(key), bytes, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(GetCacheKey(key), ct);
    }

    public async Task InvalidateHackathonListCachesAsync(CancellationToken ct = default)
    {
        // Invalidate participant list cache
        await cache.RemoveAsync(ParticipantListKey, ct);

        // Invalidate organizer caches by incrementing the version
        // This will cause all organizer list caches to miss on next request
        await IncrementCacheVersionAsync(ct);
    }

    public async Task InvalidateOrganizerListCacheAsync(Guid userId, CancellationToken ct = default)
    {
        var version = await GetCacheVersionAsync(ct);
        await cache.RemoveAsync(GetOrganizerListCacheKeyWithVersion(userId, version), ct);
    }

    public string GetParticipantListCacheKey() => ParticipantListKey;

    public async Task<string> GetOrganizerListCacheKeyAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        var version = await GetCacheVersionAsync(ct);
        return GetOrganizerListCacheKeyWithVersion(userId, version);
    }

    private static string GetOrganizerListCacheKeyWithVersion(Guid userId, long version) =>
        $"{OrganizerListKeyPrefix}{userId}:v{version}";

    private async Task<long> GetCacheVersionAsync(CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(CacheVersionKey, ct);
        if (bytes is null)
        {
            // Initialize version to 1
            await SetCacheVersionAsync(1, ct);
            return 1;
        }

        return JsonSerializer.Deserialize<long>(bytes, SerializerOptions);
    }

    private async Task SetCacheVersionAsync(long version, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(version, SerializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365),
        };
        await cache.SetAsync(CacheVersionKey, bytes, options, ct);
    }

    private async Task IncrementCacheVersionAsync(CancellationToken ct = default)
    {
        var currentVersion = await GetCacheVersionAsync(ct);
        await SetCacheVersionAsync(currentVersion + 1, ct);
    }

    private static string GetCacheKey(string key) => key;
}
