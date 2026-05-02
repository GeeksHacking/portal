using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Jint;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.Create;

public class Endpoint(ISqlSugarClient sql, IGitHubRepositoryAutomationService gitHubRepositoryAutomation)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/submissions");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Submissions"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().Includes(h => h.Activity).InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.Activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now < hackathon.SubmissionsStartDate)
        {
            AddError("Submissions are not open yet");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (now > hackathon.SubmissionsEndDate)
        {
            AddError("Submissions are closed");
            await Send.ErrorsAsync(cancellation: ct);
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
        using var engine = new Engine(options =>
        {
            options.LimitMemory(128_000_000);
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
            options.CancellationToken(ct);
        });

        // Note: totalSubmissions is kept for backward compatibility in Jint scripts,
        // but it effectively means teams selecting it now.
        var allowed = engine
            .SetValue("challenge", challenge)
            .SetValue("teamSize", teamSize)
            .SetValue("currentTeamsInChallenge", currentTeamsInChallenge)
            .SetValue("totalParticipants", totalParticipants)
            .SetValue("totalSubmissions", currentTeamsInChallenge)
            .Evaluate(challenge.SelectionCriteriaStmt)
            .ToObject();

        if (allowed is not bool boolAllowed || !boolAllowed)
        {
            AddError("Team does not meet the challenge selection criteria.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var gitHubRepositorySettings = await sql.Queryable<HackathonGitHubRepositorySettings>()
            .Where(s => s.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        try
        {
            await gitHubRepositoryAutomation.ValidateAndMaybeForkAsync(
                gitHubRepositorySettings,
                team.Name,
                req.RepoUri!,
                ct
            );
        }
        catch (GitHubRepositoryAutomationException ex)
        {
            AddError(r => r.RepoUri, ex.Message);
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
            SubmittedAt = now,
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
