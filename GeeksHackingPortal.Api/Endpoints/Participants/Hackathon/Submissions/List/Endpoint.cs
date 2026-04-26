using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/submissions");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Submissions"));
        Summary(s =>
        {
            s.Summary = "List team submissions";
            s.Description = "Retrieves all submissions for a team in the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id == req.TeamId && t.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (team is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var submissions = await sql.Queryable<ChallengeSubmission>()
            .Where(s => s.HackathonId == hackathon.Id && s.TeamId == team.Id)
            .OrderBy(s => s.SubmittedAt, OrderByType.Desc)
            .Select(s => new Response.Response_Submission
            {
                Id = s.Id,
                ChallengeId = s.ChallengeId,
                Title = s.Title,
                Summary = s.Description,
                RepoUri = s.RepositoryUri,
                DemoUri = s.DemoUri,
                SlidesUri = s.SlidesUri,
                SubmittedAt = s.SubmittedAt,
            })
            
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Submissions = submissions }, ct);
    }
}
