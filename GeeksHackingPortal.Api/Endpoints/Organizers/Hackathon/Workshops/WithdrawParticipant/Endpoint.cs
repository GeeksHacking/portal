using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.WithdrawParticipant;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}/participants/{UserId:guid}/withdraw"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Workshops").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Withdraw a participant from a workshop";
            s.Description =
                "Removes a participant from a hackathon workshop, freeing their spot.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<Workshop>()
            .FirstAsync(w => w.Id == req.WorkshopId && w.HackathonId == req.HackathonId, ct);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == req.UserId && p.HackathonId == req.HackathonId, ct);
        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
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

        await Send.OkAsync(
            new Response { Message = "Participant has been withdrawn from the workshop" },
            ct
        );
    }
}
