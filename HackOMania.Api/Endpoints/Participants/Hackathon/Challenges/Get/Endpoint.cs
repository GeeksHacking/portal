using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/challenges/{ChallengeId:guid}");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Challenges"));
        Summary(s =>
        {
            s.Summary = "Get challenge details";
            s.Description = "Retrieves public details about a challenge within a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenge = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.Id == req.ChallengeId && c.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (challenge is null || !challenge.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = challenge.Id,
                HackathonId = challenge.HackathonId,
                Title = challenge.Title,
                Description = challenge.Description,
                SelectionCriteriaStmt = challenge.SelectionCriteriaStmt,
                IsPublished = challenge.IsPublished,
            },
            ct
        );
    }
}
