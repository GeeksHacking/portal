using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using Jint;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/submissions");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Submissions"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
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

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var challenge = await sql.Queryable<Challenge>()
            .Where(c => c.Id == req.ChallengeId && c.HackathonId == hackathon.Id && c.IsPublished)
            .FirstAsync(ct);

        if (challenge is null)
        {
            AddError(r => r.ChallengeId, "Challenge not found for this hackathon.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Get all submissions for this challenge
        var challengeSubmissions = await sql.Queryable<ChallengeSubmission>()
            .Where(s => s.ChallengeId == challenge.Id)
            .Includes(s => s.TeamId)
            .ToListAsync(ct);

        // Get team information with members
        var teamWithMembers = await sql.Queryable<Team>()
            .Where(t => t.Id == team.Id)
            .Includes(t => t.Members)
            .FirstAsync(ct);

        var teamSize = teamWithMembers?.Members?.Count ?? 0;
        var currentTeamsInChallenge = challengeSubmissions.Select(s => s.TeamId).Distinct().Count();

        // Get total participants in hackathon
        var totalParticipants = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id)
            .CountAsync(ct);

        // Evaluate SelectionCriteriaStmt using Jint
        var engine = new Engine(options =>
        {
            options.LimitMemory(4_000_000);
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
            options.CancellationToken(ct);
        });

        var allowed = engine
            .SetValue("challenge", challenge)
            .SetValue("teamSize", teamSize)
            .SetValue("currentTeamsInChallenge", currentTeamsInChallenge)
            .SetValue("totalParticipants", totalParticipants)
            .SetValue("totalSubmissions", challengeSubmissions.Count)
            .Evaluate(challenge.SelectionCriteriaStmt)
            .ToObject();

        if (allowed is not bool)
        {
            throw new InvalidOperationException("SelectionCriteriaStmt did not return a boolean.");
        }

        if (allowed is false)
        {
            AddError("Team does not meet the challenge selection criteria.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var submission = new ChallengeSubmission
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathon.Id,
            TeamId = team.Id,
            ChallengeId = req.ChallengeId,
            SubmittedByUserId = userId.Value,
            Title = req.Title,
            Description = req.Summary ?? string.Empty,
            RepositoryUri = req.RepoUri ?? new Uri("https://example.com"),
            DemoUri = req.DemoUri ?? new Uri("https://example.com"),
            SlidesUri = req.SlidesUri ?? new Uri("https://example.com"),
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
                Description = submission.Description,
                RepoUri = submission.RepositoryUri,
                DemoUri = submission.DemoUri,
                SlidesUri = submission.SlidesUri,
                SubmittedAt = submission.SubmittedAt,
            },
            ct
        );
    }
}
