using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.JoinTeam;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/join");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Teams"));
        Summary(s =>
        {
            s.Summary = "Join a team";
            s.Description = "Joins the current user to a team using the team's join code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Includes(h => h.Activity)
            
            .InSingleAsync(req.HackathonId);
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

        if (!string.Equals(team.JoinCode, req.JoinCode, StringComparison.Ordinal))
        {
            AddError(r => r.JoinCode, "Invalid join code.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var participant = await membership.GetParticipant(userId.Value, hackathon.Id, ct);
        if (participant is null)
        {
            AddError("You must join the hackathon as a participant before you can join a team.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (participant.TeamId.HasValue)
        {
            AddError(r => r.JoinCode, "You are already in a team for this hackathon.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        participant.TeamId = team.Id;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { TeamId = team.Id, HackathonId = hackathon.Id }, ct);
    }
}
