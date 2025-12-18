using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.Leave;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{Id}/teams/leave");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Teams"));
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
            .Where(h => h.Id.ToString() == req.Id || h.ShortCode == req.Id)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var currentUserId = User.GetUserId<Guid>();

        var participant = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == currentUserId)
            .FirstAsync(ct);

        if (participant is null || participant.TeamId is null)
        {
            AddError("You are not currently in a team");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var teamId = participant.TeamId.Value;

        // Check if there are other team members
        var otherMembersCount = await sql.Queryable<Participant>()
            .Where(p => p.TeamId == teamId && p.UserId != currentUserId)
            .CountAsync(ct);

        // Remove participant from team
        participant.TeamId = null;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        // If no other members, delete the team
        if (otherMembersCount == 0)
        {
            await sql.Deleteable<Team>().Where(t => t.Id == teamId).ExecuteCommandAsync(ct);

            await Send.OkAsync(
                new Response
                {
                    Message = "You have left and deleted the team (you were the only member)",
                },
                ct
            );
            return;
        }

        await Send.OkAsync(new Response { Message = "You have left the team" }, ct);
    }
}
