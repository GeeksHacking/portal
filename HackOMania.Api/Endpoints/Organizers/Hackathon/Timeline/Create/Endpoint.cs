using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/timeline");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Create timeline item";
            s.Description = "Creates a new timeline item for a hackathon.";
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

        // Validate that EndTime is after StartTime
        if (req.EndTime <= req.StartTime)
        {
            AddError("EndTime must be after StartTime");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var timelineItem = new EventTimelineItem
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathon.Id,
            Title = req.Title,
            Description = req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await sql.Insertable(timelineItem).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = timelineItem.Id,
                HackathonId = timelineItem.HackathonId,
                Title = timelineItem.Title,
                Description = timelineItem.Description,
                StartTime = timelineItem.StartTime,
                EndTime = timelineItem.EndTime,
                CreatedAt = timelineItem.CreatedAt,
                UpdatedAt = timelineItem.UpdatedAt
            },
            ct
        );
    }
}
