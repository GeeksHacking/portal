using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Features.ActivityFiles;

public static class ActivityBanners
{
    /// <summary>
    /// Download URLs of the banners of the given activities, keyed by activity id. Activities without a banner
    /// are left out.
    /// </summary>
    public static async Task<Dictionary<Guid, string>> GetUrlsAsync(
        ISqlSugarClient sql,
        ActivityFileStorage storage,
        IReadOnlyCollection<Guid> activityIds,
        CancellationToken ct
    )
    {
        if (activityIds.Count == 0)
        {
            return [];
        }

        var ids = activityIds.ToList();
        var banners = await sql.Queryable<ActivityFile>()
            .Where(f => ids.Contains(f.ActivityId) && f.Kind == ActivityFileKind.Banner)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return banners
            .GroupBy(f => f.ActivityId)
            .ToDictionary(g => g.Key, g => storage.GetDownloadUrl(g.First()));
    }

    public static async Task<string?> GetUrlAsync(
        ISqlSugarClient sql,
        ActivityFileStorage storage,
        Guid activityId,
        CancellationToken ct
    )
    {
        var urls = await GetUrlsAsync(sql, storage, [activityId], ct);
        return urls.GetValueOrDefault(activityId);
    }
}
