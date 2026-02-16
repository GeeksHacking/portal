namespace HackOMania.Api.Services;

public interface IHackathonCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task InvalidateHackathonListCachesAsync(CancellationToken ct = default);
    Task InvalidateOrganizerListCacheAsync(Guid userId, CancellationToken ct = default);
    string GetParticipantListCacheKey();
    Task<string> GetOrganizerListCacheKeyAsync(Guid userId, CancellationToken ct = default);
}
