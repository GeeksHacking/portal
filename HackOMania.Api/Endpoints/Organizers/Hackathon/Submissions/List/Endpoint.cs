using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/submissions");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Submissions"));
        Summary(s =>
        {
            s.Summary = "List all submissions";
            s.Description =
                "Retrieves all submissions for a hackathon. Can filter by challenge or team.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var query = sql.Queryable<ChallengeSubmission>().Where(s => s.HackathonId == hackathon.Id);

        if (req.ChallengeId.HasValue)
        {
            query = query.Where(s => s.ChallengeId == req.ChallengeId.Value);
        }

        if (req.TeamId.HasValue)
        {
            query = query.Where(s => s.TeamId == req.TeamId.Value);
        }

        var submissions = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync(ct);

        var teamIds = submissions.Select(s => s.TeamId).Distinct().ToList();
        var challengeIds = submissions.Select(s => s.ChallengeId).Distinct().ToList();

        var teams = (
            await sql.Queryable<Team>().Where(t => teamIds.Contains(t.Id)).ToListAsync(ct)
        ).ToDictionary(t => t.Id, t => t.Name);

        var challenges = (
            await sql.Queryable<Challenge>().Where(c => challengeIds.Contains(c.Id)).ToListAsync(ct)
        ).ToDictionary(c => c.Id, c => c.Title);

        await Send.OkAsync(
            new Response
            {
                Submissions =
                [
                    .. submissions.Select(s => new SubmissionItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        SubmittedAt = s.SubmittedAt,
                        TeamId = s.TeamId,
                        TeamName = teams.GetValueOrDefault(s.TeamId, "Unknown"),
                        ChallengeId = s.ChallengeId,
                        ChallengeTitle = challenges.GetValueOrDefault(s.ChallengeId, "Unknown"),
                    }),
                ],
            },
            ct
        );
    }
}
