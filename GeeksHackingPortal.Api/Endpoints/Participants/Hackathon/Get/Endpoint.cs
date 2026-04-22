using FastEndpoints;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonIdOrShortCode}");
        AllowAnonymous();
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Get hackathon details";
            s.Description = "Retrieves public details about a hackathon by ID or short code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h =>
                h.Id.ToString() == req.HackathonIdOrShortCode
                || h.ShortCode == req.HackathonIdOrShortCode
            )
            .Includes(h => h.Activity)
            .WithCache()
            .FirstAsync(ct);

        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = hackathon.Id,
                Name = hackathon.Name,
                Description = hackathon.Description,
                Venue = hackathon.Venue,
                HomepageUri = hackathon.HomepageUri,
                ShortCode = hackathon.ShortCode,
                IsPublished = hackathon.IsPublished,
                EventStartDate = hackathon.EventStartDate,
                EventEndDate = hackathon.EventEndDate,
                SubmissionsStartDate = hackathon.SubmissionsStartDate,
                ChallengeSelectionEndDate = hackathon.ChallengeSelectionEndDate,
                SubmissionsEndDate = hackathon.SubmissionsEndDate,
                JudgingStartDate = hackathon.JudgingStartDate,
                JudgingEndDate = hackathon.JudgingEndDate,
            },
            ct
        );
    }
}
