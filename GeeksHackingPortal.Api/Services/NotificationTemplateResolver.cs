using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services;

public class NotificationTemplateResolver(ISqlSugarClient sql) : INotificationTemplateResolver
{
    public async Task<string?> ResolveTemplateIdAsync(
        Guid hackathonId,
        string eventKey,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            return null;
        }

        var normalizedEventKey = eventKey.Trim().ToLowerInvariant();

        var template = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == hackathonId && t.EventKey == normalizedEventKey)
            .FirstAsync(ct);

        return template?.TemplateId;
    }

    public async Task<Dictionary<string, string>> GetHackathonTemplatesAsync(
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == hackathonId)
            .ToListAsync(ct);

        return templates
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<Guid, Dictionary<string, string>>> GetHackathonTemplatesAsync(
        IEnumerable<Guid> hackathonIds,
        CancellationToken ct = default
    )
    {
        var ids = hackathonIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => ids.Contains(t.ActivityId))
            .ToListAsync(ct);

        return templates
            .GroupBy(t => t.ActivityId)
            .ToDictionary(
                g => g.Key,
                g =>
                    g.GroupBy(x => x.EventKey, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Last().TemplateId,
                            StringComparer.OrdinalIgnoreCase
                        )
            );
    }
}
