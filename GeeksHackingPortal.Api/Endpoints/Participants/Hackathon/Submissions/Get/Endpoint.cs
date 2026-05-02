using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.Get;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/submissions/{SubmissionId}");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Submissions"));
        Summary(s =>
        {
            s.Summary = "Get submission details";
            s.Description =
                "Retrieves detailed information about a submission. Only accessible to team members.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            
            .InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var submission = await sql.Queryable<ChallengeSubmission>()
            .Where(s => s.HackathonId == hackathon.Id && s.Id == req.SubmissionId)
            
            .FirstAsync(ct);

        if (submission is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id == submission.TeamId)
            
            .FirstAsync(ct);

        var challenge = await sql.Queryable<Challenge>()
            .Where(c => c.Id == submission.ChallengeId)
            
            .FirstAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = submission.Id,
                Title = submission.Title,
                Description = submission.Description,
                RepositoryUri = submission.RepositoryUri,
                DemoUri = submission.DemoUri,
                SlidesUri = submission.SlidesUri,
                SubmittedAt = submission.SubmittedAt,
                TeamId = submission.TeamId,
                TeamName = team.Name,
                ChallengeId = submission.ChallengeId,
                ChallengeTitle = challenge.Title,
            },
            ct
        );
    }
}
