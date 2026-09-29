using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Workshops.Join;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}/join");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Workshops"));
        Summary(s =>
        {
            s.Summary = "Join a workshop";
            s.Description = "Join a workshop as a participant.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var hackathonId = req.HackathonId;
        var workshopId = req.WorkshopId;

        // Get participant
        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == userId && p.HackathonId == hackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get workshop
        var workshop = await sql.Queryable<Workshop>()
            .Includes(w => w.Activity)
            .FirstAsync(w => w.Id == workshopId && w.HackathonId == hackathonId, ct);

        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Serialize joins per workshop so the capacity check and insert are atomic.
        // Disposing without commit rolls back.
        using var tran = sql.Ado.UseTran();
        await sql.LockRowAsync<Workshop>(workshop.Id);

        // Check if already joined
        var existingParticipant = await sql.Queryable<WorkshopParticipant>()
            .FirstAsync(
                wp => wp.WorkshopId == workshopId && wp.ParticipantId == participant.Id,
                ct
            );

        if (existingParticipant is not null)
        {
            await Send.OkAsync(
                new Response
                {
                    Id = existingParticipant.Id,
                    WorkshopId = workshop.Id,
                    WorkshopTitle = workshop.Activity.Title,
                    JoinedAt = existingParticipant.JoinedAt,
                },
                ct
            );
            return;
        }

        // Check capacity
        var joinedCount = await sql.Queryable<WorkshopParticipant>()
            .CountAsync(wp => wp.WorkshopId == workshopId, ct);
        if (joinedCount >= workshop.MaxParticipants)
        {
            AddError("Workshop is full");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var workshopParticipant = new WorkshopParticipant
        {
            Id = Guid.NewGuid(),
            WorkshopId = workshopId,
            ParticipantId = participant.Id,
            HackathonId = hackathonId,
        };

        await sql.Insertable(workshopParticipant).ExecuteCommandAsync(ct);

        tran.CommitTran();

        await Send.OkAsync(
            new Response
            {
                Id = workshopParticipant.Id,
                WorkshopId = workshop.Id,
                WorkshopTitle = workshop.Activity.Title,
                JoinedAt = workshopParticipant.JoinedAt,
            },
            ct
        );
    }
}
