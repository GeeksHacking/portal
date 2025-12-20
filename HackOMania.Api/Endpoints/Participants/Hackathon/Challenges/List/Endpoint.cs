using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Challenges.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId}/challenges");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Challenges"));
        Summary(s =>
        {
            s.Summary = "List hackathon challenges";
            s.Description = "Retrieves all published challenges for a hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await membership.FindHackathon(req.HackathonId, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenges = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.HackathonId == hackathon.Id && c.IsPublished)
            .OrderBy(c => c.CreatedAt, OrderByType.Desc)
            .Select(c => new Response.Response_Challenge
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Criteria = c.SelectionCriteriaStmt,
            })
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Challenges = challenges }, ct);
    }
}
