using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.SelectChallenge;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/challenge");
        Policies(PolicyNames.TeamMemberForHackathonTeam);
        Description(b => b.WithTags("Participants", "Teams"));
        Summary(s =>
        {
            s.Summary = "Select a challenge";
            s.Description = "Updates the team's selected challenge.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            .WithCache()
            .InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Team>()
            .Where(t => t.Id == req.TeamId && t.HackathonId == hackathon.Id)
            .WithCache()
            .FirstAsync(ct);

        if (team is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var challenge = await sql.Queryable<Challenge>()
            .Where(c => c.Id == req.ChallengeId && c.HackathonId == hackathon.Id && c.IsPublished)
            .WithCache()
            .FirstAsync(ct);

        if (challenge is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (DateTimeOffset.UtcNow < hackathon.SubmissionsStartDate)
        {
            AddError("Challenge selection is not open yet");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Check if challenge selection deadline has passed
        if (DateTimeOffset.UtcNow > hackathon.ChallengeSelectionEndDate)
        {
            AddError("Challenge selection deadline has passed");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        team.ChallengeId = challenge.Id;
        await sql.Updateable(team)
            .UpdateColumns(t => new { t.ChallengeId })
            .ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { TeamId = team.Id, ChallengeId = team.ChallengeId }, ct);
    }
}
