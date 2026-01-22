using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using Jint;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.ChallengeSelection;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/challenge-selection");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Teams"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (DateTimeOffset.UtcNow > hackathon.ChallengeSelectionEndDate)
        {
            AddError("Challenge selection period has ended.");
            await Send.ErrorsAsync(403, ct);
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

        try
        {
            sql.BeginTran();

            // Lock the challenge row to serialize selection requests for this specific challenge.
            // This prevents race conditions where multiple teams select simultaneously and exceed the limit.
            var challenge = await sql.Queryable<Challenge>()
                .With(SqlWith.UpdLock) 
                .Where(c => c.Id == req.ChallengeId && c.HackathonId == hackathon.Id && c.IsPublished)
                .FirstAsync(ct);

            if (challenge is null)
            {
                sql.RollbackTran();
                AddError(r => r.ChallengeId, "Challenge not found for this hackathon.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            // Get count of OTHER teams that have selected this challenge
            // Exclude current team to handle re-selection correctly (Problem 2)
            var currentTeamsInChallenge = await sql.Queryable<Team>()
                .Where(t => t.SelectedChallengeId == challenge.Id && t.HackathonId == hackathon.Id && t.Id != team.Id)
                .CountAsync(ct);

            // Get team information with members for size check
            var teamWithMembers = await sql.Queryable<Team>()
                .Where(t => t.Id == team.Id)
                .Includes(t => t.Members)
                .FirstAsync(ct);

            var teamSize = teamWithMembers?.Members?.Count ?? 0;

            // Get total participants in hackathon
            var totalParticipants = await sql.Queryable<Participant>()
                .Where(p => p.HackathonId == hackathon.Id)
                .CountAsync(ct);

            // Evaluate SelectionCriteriaStmt using Jint
            using var engine = new Engine(options =>
            {
                options.LimitMemory(4_000_000);
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
                sql.RollbackTran();
                AddError("Team does not meet the challenge selection criteria.");
                await Send.ErrorsAsync(400, ct);
                return;
            }

            team.SelectedChallengeId = challenge.Id;
            team.ChallengeSelectedAt = DateTimeOffset.UtcNow;

            await sql.Updateable(team)
                .UpdateColumns(t => new { t.SelectedChallengeId, t.ChallengeSelectedAt })
                .ExecuteCommandAsync(ct);

            sql.CommitTran();

            await Send.OkAsync(new Response 
            { 
                SelectedChallengeId = team.SelectedChallengeId.Value,
                SelectedAt = team.ChallengeSelectedAt.Value
            }, ct);
        }
        catch
        {
            sql.RollbackTran();
            throw;
        }
    }
}
