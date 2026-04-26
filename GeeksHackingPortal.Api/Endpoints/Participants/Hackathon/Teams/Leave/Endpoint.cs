using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.Leave;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams/leave");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Teams").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Leave current team";
            s.Description =
                "Removes the current user from their team. If they are the only member, the team will be deleted.";
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

        var currentUserId = User.GetUserId();

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == currentUserId)
            
            .FirstAsync(ct);

        if (participant?.TeamId is null)
        {
            AddError("You are not currently in a team");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var teamId = participant.TeamId.Value;

        using var tran = sql.Ado.UseTran();

        var otherMembersCount = await sql.Queryable<Participant>()
            .Where(p => p.TeamId == teamId && p.UserId != currentUserId)
            
            .CountAsync(ct);

        participant.TeamId = null;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        if (otherMembersCount == 0)
        {
            await sql.Deleteable<Team>().Where(t => t.Id == teamId).ExecuteCommandAsync(ct);

            tran.CommitTran();

            await Send.OkAsync(
                new Response
                {
                    Message = "You have left and deleted the team (you were the only member)",
                },
                ct
            );
            return;
        }

        tran.CommitTran();

        await Send.OkAsync(new Response { Message = "You have left the team" }, ct);
    }
}
