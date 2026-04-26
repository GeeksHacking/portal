using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons");
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "List organizer hackathons";
            s.Description = "Retrieves all hackathons the current user has organizer access to.";
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
        var query = sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity);

        if (!isRoot)
        {
            query = query.Where(h =>
                SqlFunc
                    .Subqueryable<Organizer>()
                    .Where(o => o.HackathonId == h.Id && o.UserId == userId)
                    .Any()
            );
        }

        var hackathons = await query.ToListAsync(ct);
        var hackathonIds = hackathons.Select(h => h.Id).ToList();

        var templates = await sql.Queryable<HackathonNotificationTemplate>()
            .Where(t => hackathonIds.Contains(t.ActivityId))
            
            .ToListAsync(ct);
        var templatesByHackathon = templates
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

        await Send.OkAsync(
            new Response
            {
                Hackathons = hackathons.Select(h => new Response.HackathonItem
                {
                    Id = h.Id,
                    Name = h.Activity.Title,
                    Description = h.Activity.Description,
                    Venue = h.Activity.Location,
                    HomepageUri = h.HomepageUri,
                    ShortCode = h.ShortCode,
                    IsPublished = h.Activity.IsPublished,
                    EventStartDate = h.Activity.StartTime,
                    EventEndDate = h.Activity.EndTime,
                    SubmissionsStartDate = h.SubmissionsStartDate,
                    ChallengeSelectionEndDate = h.ChallengeSelectionEndDate,
                    SubmissionsEndDate = h.SubmissionsEndDate,
                    JudgingStartDate = h.JudgingStartDate,
                    JudgingEndDate = h.JudgingEndDate,
                    EmailTemplates = templatesByHackathon.GetValueOrDefault(h.Id) ?? [],
                }),
            },
            ct
        );
    }
}
