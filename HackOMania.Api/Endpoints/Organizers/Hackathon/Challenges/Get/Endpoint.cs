using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/challenges/{ChallengeId:guid}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Challenges"));
        Summary(s =>
        {
            s.Summary = "Get challenge details";
            s.Description = "Retrieves detailed information about a specific challenge.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .WithCache()
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenge = await sql.Queryable<Challenge>().WithCache().InSingleAsync(req.ChallengeId);
        if (challenge is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Id = challenge.Id,
                Title = challenge.Title,
                Description = challenge.Description,
                Sponsor = challenge.Sponsor,
                SelectionCriteriaStmt = challenge.SelectionCriteriaStmt,
                IsPublished = challenge.IsPublished,
                CreatedAt = challenge.CreatedAt,
                UpdatedAt = challenge.UpdatedAt,
            },
            ct
        );
    }
}
