using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.CheckIn;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId:guid}/venue/check-in"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Venue").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Check in a participant to the venue";
            s.Description = "Checks a participant into the hackathon venue.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonId = req.HackathonId;

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == req.ParticipantUserId && p.HackathonId == hackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if already checked in (not checked out)
        var existingCheckIn = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ParticipantId == participant.Id && v.IsCheckedIn)
            .OrderByDescending(v => v.CheckInTime)
            .FirstAsync(ct);

        if (existingCheckIn is not null)
        {
            await Send.OkAsync(
                new Response
                {
                    Id = existingCheckIn.Id,
                    CheckInTime = existingCheckIn.CheckInTime.AssumeStoredAsUtc(),
                    IsCheckedIn = existingCheckIn.IsCheckedIn,
                },
                ct
            );
            return;
        }

        var checkIn = new VenueCheckIn
        {
            Id = Guid.NewGuid(),
            ParticipantId = participant.Id,
            HackathonId = hackathonId,
            CheckInTime = DateTimeOffset.UtcNow,
            IsCheckedIn = true,
        };

        await sql.Insertable(checkIn).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = checkIn.Id,
                CheckInTime = checkIn.CheckInTime.AssumeStoredAsUtc(),
                IsCheckedIn = checkIn.IsCheckedIn,
            },
            ct
        );
    }
}
