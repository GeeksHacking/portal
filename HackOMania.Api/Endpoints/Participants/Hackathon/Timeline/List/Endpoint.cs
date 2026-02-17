using FastEndpoints;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Timeline.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonIdOrShortCode}/timeline");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Get event timeline for a hackathon";
            s.Description = "Retrieves the timeline of events for a hackathon by ID or short code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h =>
                h.Id.ToString() == req.HackathonIdOrShortCode
                || h.ShortCode == req.HackathonIdOrShortCode
            )
            .FirstAsync(ct);

        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var timelineItems = await sql.Queryable<EventTimelineItem>()
            .Where(t => t.HackathonId == hackathon.Id)
            .OrderBy(t => t.StartTime)
            .WithCache()
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                TimelineItems = timelineItems
                    .Select(t => new TimelineItemDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = string.IsNullOrWhiteSpace(t.Description)
                            ? null
                            : t.Description,
                        StartTime = t.StartTime,
                        EndTime = t.EndTime,
                    })
                    .ToList(),
            },
            ct
        );
    }
}
