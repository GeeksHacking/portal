using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("organizers/activities");
        Description(b => b.WithTags("Organizers", "Activities"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var isRoot = await membership.IsRoot(userId.Value, ct);
        var query = sql.Queryable<Activity>();

        if (!isRoot)
        {
            query = query.Where(a =>
                SqlFunc
                    .Subqueryable<ActivityOrganizer>()
                    .Where(o => o.ActivityId == a.Id && o.UserId == userId.Value)
                    .Any()
            );
        }

        var activities = await query.ToListAsync(ct);
        var activityIds = activities.Select(a => a.Id).ToList();
        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => activityIds.Contains(t.ActivityId))
            .ToListAsync(ct);
        var templatesByActivity = templates
            .GroupBy(t => t.ActivityId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.EventKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Last().TemplateId, StringComparer.OrdinalIgnoreCase)
            );

        await Send.OkAsync(
            new Response
            {
                Activities =
                [
                    .. activities.Select(a => new ActivityItem
                    {
                        Id = a.Id,
                        Kind = a.Kind.ToString(),
                        Title = a.Title,
                        Description = a.Description,
                        Location = a.Location,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        IsPublished = a.IsPublished,
                        EmailTemplates = templatesByActivity.GetValueOrDefault(a.Id) ?? [],
                    }),
                ],
            },
            ct
        );
    }
}
