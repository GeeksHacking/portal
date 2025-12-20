using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Teams.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId}/teams");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Teams"));
        Summary(s =>
        {
            s.Summary = "List all teams (Organizer)";
            s.Description = "Retrieves all teams for a hackathon. Requires organizer access.";
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

        var teams = await sql.Queryable<Entities.Team>()
            .Where(t => t.HackathonId == hackathon.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var teamIds = teams.Select(t => t.Id).ToList();

        // Get member counts
        var memberCounts = await sql.Queryable<Entities.Participant>()
            .Where(p => p.TeamId != null && teamIds.Contains(p.TeamId.Value))
            .GroupBy(p => p.TeamId)
            .Select(g => new { TeamId = g.TeamId, Count = SqlFunc.AggregateCount(g.UserId) })
            .ToListAsync(ct);

        var memberCountDict = memberCounts.ToDictionary(m => m.TeamId!.Value, m => m.Count);

        await Send.OkAsync(
            new Response
            {
                Teams = teams
                    .Select(t => new TeamItem
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        CreatedAt = t.CreatedAt,
                        MemberCount = memberCountDict.GetValueOrDefault(t.Id, 0),
                    })
                    .ToList(),
            },
            ct
        );
    }
}
