using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Venue.CheckOut;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/venue/check-out");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Venue"));
        Summary(s =>
        {
            s.Summary = "Check out from venue";
            s.Description = "Check out from the hackathon venue.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var hackathonId = req.HackathonId;

        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == userId && p.HackathonId == hackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var checkIn = await sql.Queryable<VenueCheckIn>()
            .Where(v =>
                v.ActivityRegistrationId == participant.Id
                && v.ActivityId == hackathonId
                && v.IsCheckedIn
            )
            .OrderByDescending(v => v.CheckInTime)
            .FirstAsync(ct);

        if (checkIn is null)
        {
            AddError("Not checked in");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        checkIn.CheckOutTime = DateTimeOffset.UtcNow;
        checkIn.IsCheckedIn = false;

        await sql.Updateable(checkIn).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = checkIn.Id,
                CheckOutTime = checkIn.CheckOutTime.Value.AssumeStoredAsUtc(),
                IsCheckedIn = checkIn.IsCheckedIn,
            },
            ct
        );
    }
}
