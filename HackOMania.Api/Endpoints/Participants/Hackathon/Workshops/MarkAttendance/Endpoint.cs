using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Workshops.MarkAttendance;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/workshops/{WorkshopId:guid}/attendance");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Workshops"));
        Summary(s =>
        {
            s.Summary = "Mark attendance";
            s.Description = "Mark attendance for a workshop that the participant has joined.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var hackathonId = req.HackathonId;
        var workshopId = req.WorkshopId;

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == userId && p.HackathonId == hackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var workshopParticipant = await sql.Queryable<WorkshopParticipant>()
            .FirstAsync(
                wp => wp.WorkshopId == workshopId && wp.ParticipantId == participant.Id,
                ct
            );

        if (workshopParticipant is null)
        {
            AddError("Not joined this workshop");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        workshopParticipant.HasAttended = true;
        workshopParticipant.AttendedAt = DateTimeOffset.UtcNow;

        await sql.Updateable(workshopParticipant).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                HasAttended = workshopParticipant.HasAttended,
                AttendedAt = workshopParticipant.AttendedAt.Value,
            },
            ct
        );
    }
}
