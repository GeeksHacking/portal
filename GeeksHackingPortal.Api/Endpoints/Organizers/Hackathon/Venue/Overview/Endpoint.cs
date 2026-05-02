using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Venue.Overview;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(
            "organizers/hackathons/{ActivityId:guid}/venue/overview",
            "organizers/standalone-workshops/{ActivityId:guid}/venue/overview"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Venue"));
        Summary(s =>
        {
            s.Summary = "Get venue check-in overview";
            s.Description =
                "Get an overview of all participant check-ins/check-outs for the activity.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Get all participants
        var participants = await sql.Queryable<ActivityRegistration>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where(p => p.ActivityId == req.ActivityId)
            
            .Select((p, u) => new { Participant = p, User = u })
            .ToListAsync(ct);

        // NOTE: VenueCheckIn data changes frequently during events, which may cause cache invalidation issues.
        // See CACHING.md for details. Consider removing  or implementing a short TTL (5-30 seconds)
        // if stale data becomes problematic during high-traffic event periods.
        var checkIns = await sql.Queryable<VenueCheckIn>()
            .Where(v => v.ActivityId == req.ActivityId)
            
            .ToListAsync(ct);

        var participantDtos = participants
            .Select(p =>
            {
                var participantCheckIns = checkIns
                    .Where(c => c.ActivityRegistrationId == p.Participant.Id)
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
                    LastCheckInTime = lastCheckIn?.CheckInTime.AssumeStoredAsUtc(),
                    LastCheckOutTime = lastCheckIn?.CheckOutTime.AssumeStoredAsUtc(),
                    TotalCheckIns = participantCheckIns.Count,
                };
            })
            .ToList();

        var userNameByParticipantId = participants.ToDictionary(
            p => p.Participant.Id,
            p => p.User.FirstName + " " + p.User.LastName
        );
        var userIdByParticipantId = participants.ToDictionary(p => p.Participant.Id, p => p.Participant.UserId);

        var auditTrail = checkIns
            .SelectMany(c =>
            {
                var participantId = c.ActivityRegistrationId;
                var userName = userNameByParticipantId.GetValueOrDefault(participantId, "Unknown");
                var events = new List<VenueAuditTrailItemDto>
                {
                    new()
                    {
                        ParticipantId = participantId,
                        UserId = userIdByParticipantId.GetValueOrDefault(participantId),
                        UserName = userName,
                        Action = "checked in",
                        Timestamp = c.CheckInTime.AssumeStoredAsUtc(),
                    },
                };

                if (c.CheckOutTime.HasValue)
                {
                    events.Add(
                        new VenueAuditTrailItemDto
                        {
                            ParticipantId = participantId,
                            UserId = userIdByParticipantId.GetValueOrDefault(participantId),
                            UserName = userName,
                            Action = "checked out",
                            Timestamp = c.CheckOutTime.Value.AssumeStoredAsUtc(),
                        }
                    );
                }

                return events;
            })
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToList();

        await Send.OkAsync(new Response { Participants = participantDtos, AuditTrail = auditTrail }, ct);
    }
}
