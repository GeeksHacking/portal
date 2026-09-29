using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.Withdraw;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}/withdraw");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Workshops").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Withdraw from a workshop";
            s.Description =
                "Withdraws the current participant from a hackathon workshop they joined, freeing their spot.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == userId && p.HackathonId == req.HackathonId, ct);
        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var workshop = await sql.Queryable<Workshop>()
            .Includes(w => w.Activity)
            .FirstAsync(w => w.Id == req.WorkshopId && w.HackathonId == req.HackathonId, ct);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (workshop.Activity.EndTime < DateTimeOffset.UtcNow)
        {
            AddError("You cannot leave a workshop after it has ended");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var deleted = await sql.Deleteable<WorkshopParticipant>()
            .Where(wp => wp.WorkshopId == workshop.Id && wp.ParticipantId == participant.Id)
            .ExecuteCommandAsync(ct);
        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new Response { Message = "You have withdrawn from the workshop" }, ct);
    }
}
