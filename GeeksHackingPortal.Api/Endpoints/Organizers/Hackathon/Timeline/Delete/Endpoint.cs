using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Timeline.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
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

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var timelineItem = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.Id == req.TimelineItemId && t.ActivityId == hackathon.ActivityId)
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
