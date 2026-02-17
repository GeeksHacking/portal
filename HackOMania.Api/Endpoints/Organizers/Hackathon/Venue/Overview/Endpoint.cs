using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Venue.Overview;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/venue/overview");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Venue"));
        Summary(s =>
        {
            s.Summary = "Get venue check-in overview";
            s.Description =
                "Get an overview of all participant check-ins/check-outs for the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathonId = req.HackathonId;

        // Get all participants
        var participants = await sql.Queryable<Participant>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where(p => p.HackathonId == hackathonId)
            .WithCache()
            .Select((p, u) => new { Participant = p, User = u })
            .ToListAsync(ct);

        // NOTE: VenueCheckIn data changes frequently during events, which may cause cache invalidation issues.
        // See CACHING.md for details. Consider removing .WithCache() or implementing a short TTL (5-30 seconds)
        // if stale data becomes problematic during high-traffic event periods.
        var checkIns = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.HackathonId == hackathonId)
            .WithCache()
            .ToListAsync(ct);

        var participantDtos = participants
            .Select(p =>
            {
                var participantCheckIns = checkIns
                    .Where(c => c.ParticipantId == p.Participant.Id)
                    .ToList();
                var lastCheckIn = participantCheckIns
                    .OrderByDescending(c => c.CheckInTime)
                    .FirstOrDefault();

                return new ParticipantCheckInDto
                {
                    ParticipantId = p.Participant.Id,
                    UserId = p.Participant.UserId,
                    UserName = p.User.FirstName + " " + p.User.LastName,
                    IsCurrentlyCheckedIn = lastCheckIn?.IsCheckedIn ?? false,
                    LastCheckInTime = lastCheckIn?.CheckInTime,
                    LastCheckOutTime = lastCheckIn?.CheckOutTime,
                    TotalCheckIns = participantCheckIns.Count,
                };
            })
            .ToList();

        await Send.OkAsync(new Response { Participants = participantDtos }, ct);
    }
}
