using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId}/challenges/{ChallengeId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Challenges"));
        Summary(s =>
        {
            s.Summary = "Get challenge details (Organizer)";
            s.Description =
                "Retrieves detailed information about a specific challenge. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id == req.HackathonId)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenge = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.HackathonId == hackathon.Id && c.Id.ToString() == req.ChallengeId)
            .FirstAsync(ct);

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
                SelectionCriteriaStmt = challenge.SelectionCriteriaStmt,
                IsPublished = challenge.IsPublished,
                CreatedAt = challenge.CreatedAt,
                UpdatedAt = challenge.UpdatedAt,
            },
            ct
        );
    }
}
