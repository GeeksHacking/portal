using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("organizers/hackathons/{Id}/challenges/{ChallengeId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Challenges"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id.ToString() == req.Id)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenge = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.Id.ToString() == req.ChallengeId && c.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (challenge is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Title))
        {
            challenge.Title = req.Title;
        }

        if (!string.IsNullOrWhiteSpace(req.Description))
        {
            challenge.Description = req.Description;
        }

        if (req.Criteria is not null)
        {
            challenge.SelectionCriteriaStmt = req.Criteria;
        }

        if (req.IsPublished.HasValue)
        {
            challenge.IsPublished = req.IsPublished.Value;
        }

        challenge.UpdatedAt = DateTimeOffset.UtcNow;

        await sql.Updateable(challenge).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = challenge.Id,
                HackathonId = challenge.HackathonId,
                Title = challenge.Title,
                Description = challenge.Description,
                Criteria = challenge.SelectionCriteriaStmt,
                IsPublished = challenge.IsPublished,
                UpdatedAt = challenge.UpdatedAt,
            },
            ct
        );
    }
}
