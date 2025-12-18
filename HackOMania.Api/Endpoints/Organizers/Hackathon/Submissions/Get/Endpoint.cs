using FastEndpoints;
using HackOMania.Api.Authorization;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{Id}/submissions/{SubmissionId}");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Submissions"));
        Summary(s =>
        {
            s.Summary = "Get submission details (Organizer)";
            s.Description =
                "Retrieves detailed information about a specific submission. Requires organizer access.";
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

        var submission = await sql.Queryable<Entities.ChallengeSubmission>()
            .Where(s => s.HackathonId == hackathon.Id && s.Id == req.SubmissionId)
            .FirstAsync(ct);

        if (submission is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Entities.Team>()
            .Where(t => t.Id == submission.TeamId)
            .FirstAsync(ct);

        var challenge = await sql.Queryable<Entities.Challenge>()
            .Where(c => c.Id == submission.ChallengeId)
            .FirstAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = submission.Id,
                Title = submission.Title,
                Description = submission.Description,
                Location = submission.Location,
                DevpostUri = submission.DevpostUri,
                RepositoryUri = submission.RepositoryUri,
                DemoUri = submission.DemoUri,
                SlidesUri = submission.SlidesUri,
                SubmittedAt = submission.SubmittedAt,
                TeamId = submission.TeamId,
                TeamName = team?.Name ?? "Unknown",
                ChallengeId = submission.ChallengeId,
                ChallengeTitle = challenge?.Title,
            },
            ct
        );
    }
}
