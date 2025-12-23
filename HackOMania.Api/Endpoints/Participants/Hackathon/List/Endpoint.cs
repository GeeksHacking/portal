using FastEndpoints;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.List;

public class Endpoint(ISqlSugarClient sql) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("participants/hackathons");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "List all hackathons";
            s.Description = "Retrieves all published hackathons.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var hackathons = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.IsPublished)
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Hackathons = hackathons.Select(h => new Response.HackathonItem
                {
                    Id = h.Id,
                    Name = h.Name,
                    Description = h.Description,
                    Venue = h.Venue,
                    HomepageUri = h.HomepageUri,
                    ShortCode = h.ShortCode,
                    IsPublished = h.IsPublished,
                    EventStartDate = h.EventStartDate,
                    EventEndDate = h.EventEndDate,
                    SubmissionsStartDate = h.SubmissionsStartDate,
                    SubmissionsEndDate = h.SubmissionsEndDate,
                    JudgingStartDate = h.JudgingStartDate,
                    JudgingEndDate = h.JudgingEndDate,
                }),
            },
            ct
        );
    }
}
