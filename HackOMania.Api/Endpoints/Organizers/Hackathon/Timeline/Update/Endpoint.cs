using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{HackathonId:guid}/timeline/{TimelineItemId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Update timeline item";
            s.Description = "Updates an existing timeline item for a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var timelineItem = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.Id == req.TimelineItemId && t.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (timelineItem is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            timelineItem.Title = req.Title;
        }

        if (req.Description is not null)
        {
            timelineItem.Description = req.Description;
        }

        if (req.StartTime.HasValue)
        {
            timelineItem.StartTime = req.StartTime.Value;
        }

        if (req.EndTime.HasValue)
        {
            timelineItem.EndTime = req.EndTime.Value;
        }

        timelineItem.UpdatedAt = DateTimeOffset.UtcNow;

        await sql.Updateable(timelineItem).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = timelineItem.Id,
                HackathonId = timelineItem.HackathonId,
                Title = timelineItem.Title,
                Description = timelineItem.Description,
                StartTime = timelineItem.StartTime,
                EndTime = timelineItem.EndTime,
                UpdatedAt = timelineItem.UpdatedAt
            },
            ct
        );
    }
}
