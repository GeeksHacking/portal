using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Timeline.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/hackathons/{HackathonId:guid}/timeline");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Timeline"));
        Summary(s =>
        {
            s.Summary = "Create timeline item";
            s.Description = "Creates a new timeline item for a hackathon.";
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
            ActivityId = hackathon.Id,
            Title = req.Title,
            Description = string.IsNullOrWhiteSpace(req.Description)
                ? string.Empty
                : req.Description,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(timelineItem).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = timelineItem.Id,
                HackathonId = hackathon.Id,
                Title = timelineItem.Title,
                Description = string.IsNullOrWhiteSpace(timelineItem.Description)
                    ? null
                    : timelineItem.Description,
                StartTime = timelineItem.StartTime,
                EndTime = timelineItem.EndTime,
                CreatedAt = timelineItem.CreatedAt,
                UpdatedAt = timelineItem.UpdatedAt,
            },
            ct
        );
    }
}
