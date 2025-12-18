using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.List;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/hackathons/{Id}/teams/{TeamId}/submissions");
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
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var hackathon = await membership.FindHackathon(req.Id, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id.ToString() == req.TeamId && t.HackathonId == hackathon.Id)
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
                Location = s.Location,
                DevpostUri = s.DevpostUri,
                RepoUri = s.RepositoryUri,
                DemoUri = s.DemoUri,
                SlidesUri = s.SlidesUri,
                SubmittedAt = s.SubmittedAt,
            })
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Submissions = submissions }, ct);
    }
}
