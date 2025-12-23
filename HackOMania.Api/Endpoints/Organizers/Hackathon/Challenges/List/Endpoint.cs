using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Challenges.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/challenges");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Challenges"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenges = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.HackathonId == hackathon.Id)
            .OrderBy(c => c.CreatedAt, OrderByType.Desc)
            .Select(c => new Response.ResponseChallenge
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Criteria = c.SelectionCriteriaStmt,
                IsPublished = c.IsPublished,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Challenges = challenges }, ct);
    }
}
