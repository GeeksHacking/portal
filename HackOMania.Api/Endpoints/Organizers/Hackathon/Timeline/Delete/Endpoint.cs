using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Delete;

public class Endpoint(ISqlSugarClient sql) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("organizers/hackathons/{HackathonId:guid}/timeline/{TimelineItemId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Delete timeline item";
            s.Description = "Deletes a timeline item from a hackathon.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var hackathonId = Route<Guid>("HackathonId");
        var timelineItemId = Route<Guid>("TimelineItemId");

        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(hackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var timelineItem = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.Id == timelineItemId && t.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (timelineItem is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await sql.Deleteable(timelineItem).ExecuteCommandAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
