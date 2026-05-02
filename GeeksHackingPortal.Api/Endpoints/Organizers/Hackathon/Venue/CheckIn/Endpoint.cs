using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.CheckIn;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{ActivityId:guid}/participants/{ParticipantUserId:guid}/venue/check-in",
            "organizers/standalone-workshops/{ActivityId:guid}/participants/{ParticipantUserId:guid}/venue/check-in"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Venue").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Check in a participant to the venue";
            s.Description = "Checks a participant into the activity venue.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var participant = await sql.Queryable<ActivityRegistration>()
            .FirstAsync(
                p =>
                    p.UserId == req.ParticipantUserId
                    && p.ActivityId == req.ActivityId
                    && p.Status == ActivityRegistrationStatus.Registered
                    && p.WithdrawnAt == null,
                ct
            );

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Check if already checked in (not checked out)
        var existingCheckIn = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ActivityRegistrationId == participant.Id && v.ActivityId == req.ActivityId && v.IsCheckedIn)
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
            ActivityRegistrationId = participant.Id,
            ActivityId = req.ActivityId,
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
