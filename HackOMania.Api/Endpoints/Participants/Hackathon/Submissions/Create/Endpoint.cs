using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{Id}/teams/{TeamId}/submissions");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Submissions"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var id = req.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var hackathon = await membership.FindHackathon(id, ct);
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

        var userId = User.GetUserId<Guid>();

        if (req.ChallengeId.HasValue)
        {
            var challengeExists = await sql.Queryable<Challenge>()
                .AnyAsync(
                    c =>
                        c.Id == req.ChallengeId.Value
                        && c.HackathonId == hackathon.Id
                        && c.IsPublished,
                    ct
                );

            if (!challengeExists)
            {
                AddError(r => r.ChallengeId, "Challenge not found for this hackathon.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        var submission = new ChallengeSubmission
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathon.Id,
            TeamId = team.Id,
            ChallengeId = req.ChallengeId,
            SubmittedByUserId = userId,
            Title = req.Title,
            Description = req.Summary,
            Location = req.Location,
            DevpostUri = req.DevpostUri,
            RepositoryUri = req.RepoUri,
            DemoUri = req.DemoUri,
            SlidesUri = req.SlidesUri,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(submission).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = submission.Id,
                HackathonId = submission.HackathonId,
                TeamId = submission.TeamId,
                ChallengeId = submission.ChallengeId,
                Title = submission.Title,
                Summary = submission.Description,
                Location = submission.Location,
                DevpostUri = submission.DevpostUri,
                RepoUri = submission.RepositoryUri,
                DemoUri = submission.DemoUri,
                SlidesUri = submission.SlidesUri,
                SubmittedAt = submission.SubmittedAt,
            },
            ct
        );
    }
}
