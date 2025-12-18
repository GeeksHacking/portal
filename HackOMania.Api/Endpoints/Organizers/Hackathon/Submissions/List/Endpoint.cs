using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{Id}/submissions");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Submissions"));
        Summary(s =>
        {
            s.Summary = "List all submissions (Organizer)";
            s.Description =
                "Retrieves all submissions for a hackathon. Can filter by challenge or team. Requires organizer access.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.Id.ToString() == req.Id || h.ShortCode == req.Id)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var query = sql.Queryable<Entities.ChallengeSubmission>()
            .Where(s => s.HackathonId == hackathon.Id);

        if (req.ChallengeId.HasValue)
        {
            query = query.Where(s => s.ChallengeId == req.ChallengeId.Value);
        }

        if (req.TeamId.HasValue)
        {
            query = query.Where(s => s.TeamId == req.TeamId.Value);
        }

        var submissions = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync(ct);

        // Fetch related data
        var teamIds = submissions.Select(s => s.TeamId).Distinct().ToList();
        var challengeIds = submissions
            .Where(s => s.ChallengeId.HasValue)
            .Select(s => s.ChallengeId!.Value)
            .Distinct()
            .ToList();

        var teams = (
            await sql.Queryable<Entities.Team>().Where(t => teamIds.Contains(t.Id)).ToListAsync(ct)
        ).ToDictionary(t => t.Id, t => t.Name);

        var challenges = (
            await sql.Queryable<Entities.Challenge>()
                .Where(c => challengeIds.Contains(c.Id))
                .ToListAsync(ct)
        ).ToDictionary(c => c.Id, c => c.Title);

        await Send.OkAsync(
            new Response
            {
                Submissions = submissions
                    .Select(s => new SubmissionItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        SubmittedAt = s.SubmittedAt,
                        TeamId = s.TeamId,
                        TeamName = teams.GetValueOrDefault(s.TeamId, "Unknown"),
                        ChallengeId = s.ChallengeId,
                        ChallengeTitle = s.ChallengeId.HasValue
                            ? challenges.GetValueOrDefault(s.ChallengeId.Value, "Unknown")
                            : null,
                    })
                    .ToList(),
            },
            ct
        );
    }
}
