using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Teams.RemoveMember;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Delete("participants/hackathons/{HackathonId:guid}/teams/{TeamId:guid}/members/{UserId:guid}");
        Policies(PolicyNames.TeamCreatorForHackathonTeam);
        Description(b => b.WithTags("Participants", "Teams"));
        Summary(s =>
        {
            s.Summary = "Remove a member from team";
            s.Description =
                "Allows the team creator to remove another member from the team. Cannot remove yourself (use leave endpoint instead). If the removed member is the last one, the team will be deleted.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var currentUserId = User.GetUserId();

        // Prevent removing yourself - use leave endpoint instead
        if (req.UserId == currentUserId)
        {
            AddError("Cannot remove yourself from the team. Use the leave endpoint instead.");
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

        // Find the participant to remove
        var participantToRemove = await sql.Queryable<Participant>()
            .Where(p =>
                p.HackathonId == hackathon.Id && p.UserId == req.UserId && p.TeamId == team.Id
            )
            .FirstAsync(ct);

        if (participantToRemove is null)
        {
            AddError("User is not a member of this team");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Remove the member from the team
        participantToRemove.TeamId = null;
        await sql.Updateable(participantToRemove).ExecuteCommandAsync(ct);

        // Check if there are any remaining members
        var remainingMembersCount = await sql.Queryable<Participant>()
            .Where(p => p.TeamId == team.Id)
            .CountAsync(ct);

        // If no members remain, delete the team
        if (remainingMembersCount == 0)
        {
            await sql.Deleteable<Team>().Where(t => t.Id == team.Id).ExecuteCommandAsync(ct);

            await Send.OkAsync(
                new Response
                {
                    Message =
                        "Member removed successfully. Team has been deleted as no members remain.",
                },
                ct
            );
            return;
        }

        await Send.OkAsync(new Response { Message = "Member removed successfully" }, ct);
    }
}
