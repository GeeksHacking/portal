using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.JoinTeam;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId}/teams/{TeamId}/join");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Teams"));
        Summary(s =>
        {
            s.Summary = "Join a team";
            s.Description = "Joins the current user to a team using the team's join code.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await membership.FindHackathon(req.HackathonId, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var team = await sql.Queryable<Entities.Team>()
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
