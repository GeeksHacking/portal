using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops");
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
        Summary(s =>
        {
            s.Summary = "List organizer standalone workshops";
            s.Description = "Retrieves all standalone workshops the current user can manage.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var isRoot = await membership.IsRoot(userId.Value, ct);
        var query = sql.Queryable<StandaloneWorkshop>().Includes(w => w.Activity);

        if (!isRoot)
        {
            query = query.Where(w =>
                SqlFunc
                    .Subqueryable<ActivityOrganizer>()
                    .Where(o => o.ActivityId == w.Id && o.UserId == userId.Value)
                    .Any()
            );
        }

        var workshops = await query.ToListAsync(ct);
        var workshopIds = workshops.Select(w => w.Id).ToList();

        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => workshopIds.Contains(t.ActivityId))
            .ToListAsync(ct);
        var templatesByWorkshop = templates
            .GroupBy(t => t.ActivityId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.EventKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Last().TemplateId, StringComparer.OrdinalIgnoreCase)
            );

        await Send.OkAsync(
            new Response
            {
                StandaloneWorkshops = workshops.Select(w => new Response.StandaloneWorkshopItem
                {
                    Id = w.Id,
                    Title = w.Activity.Title,
                    Description = w.Activity.Description,
                    StartTime = w.Activity.StartTime,
                    EndTime = w.Activity.EndTime,
                    Location = w.Activity.Location,
                    HomepageUri = w.HomepageUri,
                    ShortCode = w.ShortCode,
                    MaxParticipants = w.MaxParticipants,
                    IsPublished = w.Activity.IsPublished,
                    CreatedAt = w.Activity.CreatedAt,
                    EmailTemplates = templatesByWorkshop.GetValueOrDefault(w.Id) ?? [],
                }),
            },
            ct
        );
    }
}
