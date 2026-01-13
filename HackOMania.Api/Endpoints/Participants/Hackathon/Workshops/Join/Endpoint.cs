using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.Join;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}/join");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Workshops"));
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
            .Includes(w => w.Participants)
            .FirstAsync(w => w.Id == workshopId && w.HackathonId == hackathonId, ct);

        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
                    WorkshopTitle = workshop.Title,
                    JoinedAt = existingParticipant.JoinedAt,
                },
                ct
            );
            return;
        }

        // Check capacity
        if (workshop.Participants.Count >= workshop.MaxParticipants)
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

        await Send.OkAsync(
            new Response
            {
                Id = workshopParticipant.Id,
                WorkshopId = workshop.Id,
                WorkshopTitle = workshop.Title,
                JoinedAt = workshopParticipant.JoinedAt,
            },
            ct
        );
    }
}
