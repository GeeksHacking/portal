using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Challenges.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/challenges");
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
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenges = await sql.Queryable<Challenge>()
            .Where(c => c.HackathonId == hackathon.Id && c.IsPublished)
            .LeftJoin<Team>((c, t) => c.Id == t.ChallengeId)
            .GroupBy(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.Sponsor,
                c.SelectionCriteriaStmt,
                c.CreatedAt,
            })
            .OrderBy(c => c.CreatedAt, OrderByType.Desc)
            .Select(
                (c, t) =>
                    new Response.ChallengeItem
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Description = c.Description,
                        Sponsor = c.Sponsor,
                        SelectionCriteriaStmt = c.SelectionCriteriaStmt,
                        TeamCount = SqlFunc.AggregateCount(t.Id),
                    }
            )
            .WithCache()
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Challenges = challenges }, ct);
    }
}
