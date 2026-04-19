using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.CheckOut;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId:guid}/venue/check-out"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Venue").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Check out a participant from the venue";
            s.Description = "Checks a participant out from the hackathon venue.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var participant = await sql.Queryable<Participant>()
            .FirstAsync(p => p.UserId == req.ParticipantUserId && p.HackathonId == req.HackathonId, ct);

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var checkIn = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ParticipantId == participant.Id && v.IsCheckedIn)
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
