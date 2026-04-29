using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities;

public class GetEndpoint(ISqlSugarClient sql) : Endpoint<GetRequest, Response>
{
    public override void Configure()
    {
        Get("organizers/activities/{ActivityId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Activities"));
    }

    public override async Task HandleAsync(GetRequest req, CancellationToken ct)
    {
        var activity = await sql.Queryable<Activity>().InSingleAsync(req.ActivityId);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var templates = await LoadTemplates(req.ActivityId, ct);

        await Send.OkAsync(ToResponse(activity, templates), ct);
    }

    private async Task<Dictionary<string, string>> LoadTemplates(Guid activityId, CancellationToken ct)
    {
        var persisted = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == activityId)
            .ToListAsync(ct);
        return persisted
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);
    }

    private static Response ToResponse(Activity activity, Dictionary<string, string> templates) =>
        new()
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Location = activity.Location,
            IsPublished = activity.IsPublished,
            Kind = activity.Kind.ToString(),
            EmailTemplates = templates,
        };
}

public class UpdateEndpoint(ISqlSugarClient sql) : Endpoint<UpdateRequest, Response>
{
    public override void Configure()
    {
        Patch("organizers/activities/{ActivityId:guid}");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Activities"));
    }

    public override async Task HandleAsync(UpdateRequest req, CancellationToken ct)
    {
        var activity = await sql.Queryable<Activity>().InSingleAsync(req.ActivityId);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            activity.Title = req.Title;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            activity.Description = req.Description;
        }

        if (req.StartTime.HasValue)
        {
            activity.StartTime = req.StartTime.Value;
        }

        if (req.EndTime.HasValue)
        {
            activity.EndTime = req.EndTime.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Location))
        {
            activity.Location = req.Location;
        }

        if (req.IsPublished.HasValue)
        {
            activity.IsPublished = req.IsPublished.Value;
        }

        var normalizedTemplates = req.EmailTemplates is null
            ? null
            : EmailTemplateNormalizer.Normalize(req.EmailTemplates);

        if (normalizedTemplates is not null)
        {
            await sql.Deleteable<HackathonNotificationTemplate>()
                .Where(t => t.ActivityId == activity.Id)
                .ExecuteCommandAsync(ct);

            if (normalizedTemplates.Count > 0)
            {
                var inserts = normalizedTemplates.Select(kvp => new HackathonNotificationTemplate
                {
                    Id = Guid.NewGuid(),
                    ActivityId = activity.Id,
                    EventKey = kvp.Key,
                    TemplateId = kvp.Value,
                });
                await sql.Insertable(inserts.ToList()).ExecuteCommandAsync(ct);
            }
        }

        activity.UpdatedAt = DateTimeOffset.UtcNow;
        await sql.Updateable(activity).ExecuteCommandAsync(ct);

        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => t.ActivityId == activity.Id)
            .ToListAsync(ct);
        var templateMap = templates
            .GroupBy(t => t.EventKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().TemplateId, StringComparer.OrdinalIgnoreCase);

        await Send.OkAsync(ToResponse(activity, templateMap), ct);
    }

    private static Response ToResponse(Activity activity, Dictionary<string, string> templates) =>
        new()
        {
            Id = activity.Id,
            Title = activity.Title,
            Description = activity.Description,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Location = activity.Location,
            IsPublished = activity.IsPublished,
            Kind = activity.Kind.ToString(),
            EmailTemplates = templates,
        };
}

internal static class EmailTemplateNormalizer
{
    public static Dictionary<string, string> Normalize(Dictionary<string, string>? templates)
    {
        if (templates is null)
        {
            return [];
        }

        return templates
            .Where(kvp =>
                !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value)
            )
            .GroupBy(kvp => kvp.Key.Trim().ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last().Value.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
