using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Teams.Create;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/teams");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Teams"));
        Summary(s =>
        {
            s.Summary = "Create a team";
            s.Description =
                "Creates a new team for the hackathon and adds the current user as a member.";
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

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var participant = await membership.GetParticipant(userId.Value, hackathon.Id, ct);

        if (participant is null)
        {
            AddError("You must join the hackathon as a participant before you can create a team.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (participant.TeamId.HasValue)
        {
            AddError(r => r.Name, "You are already in a team for this hackathon.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Description = req.Description,
            HackathonId = hackathon.Id,
            CreatedByUserId = userId.Value,
        };

        using var tran = sql.Ado.UseTran();

        await sql.Insertable(team).ExecuteCommandAsync(ct);

        participant.TeamId = team.Id;
        await sql.Updateable(participant).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.OkAsync(
            new Response
            {
                Id = team.Id,
                HackathonId = team.HackathonId,
                Name = team.Name,
                Description = team.Description,
                JoinCode = team.JoinCode,
            },
            ct
        );
    }
}
