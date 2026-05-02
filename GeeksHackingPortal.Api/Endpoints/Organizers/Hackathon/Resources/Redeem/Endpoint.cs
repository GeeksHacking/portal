using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using Jint;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Redeem;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{ActivityId:guid}/participants/{ParticipantUserId:guid}/resources/{ResourceId:guid}/redemptions",
            "organizers/standalone-workshops/{ActivityId:guid}/participants/{ParticipantUserId:guid}/resources/{ResourceId:guid}/redemptions"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Resources").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Redeem a resource for a participant";
            s.Description =
                "Creates a redemption record for an activity resource on behalf of a participant.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var activity = await sql.Queryable<Activity>().InSingleAsync(req.ActivityId);
        if (activity is null || !activity.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = await sql.Queryable<Resource>()
            .Includes(r => r.Redemptions)
            .Where(r => r.Id == req.ResourceId && r.ActivityId == req.ActivityId && r.IsPublished)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var registration = await sql.Queryable<ActivityRegistration>()
            .FirstAsync(
                r =>
                    r.UserId == req.ParticipantUserId
                    && r.ActivityId == req.ActivityId
                    && r.Status == ActivityRegistrationStatus.Registered
                    && r.WithdrawnAt == null,
                ct
            );

        if (registration is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participant = await sql.Queryable<Participant>()
            .Includes(p => p.Team, t => t.Members)
            .FirstAsync(p => p.Id == registration.Id, ct);

        // Count redemptions by this participant
        var participantRedemptions =
            resource.Redemptions?.Count(r => r.UserId == req.ParticipantUserId) ?? 0;

        // Count redemptions by this participant's team (if they have one)
        int teamRedemptions = 0;
        int teamSize = 0;
        if (participant?.TeamId.HasValue == true && participant.Team is not null)
        {
            var teamMemberIds =
                participant.Team.Members?.Select(m => m.UserId).ToList() ?? new List<Guid>();
            teamRedemptions =
                resource.Redemptions?.Count(r => teamMemberIds.Contains(r.UserId)) ?? 0;
            teamSize = participant.Team.Members?.Count ?? 0;
        }

        var engine = new Engine(options =>
        {
            options.LimitMemory(4_000_000);
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
            options.CancellationToken(ct);
        });

        // Set evaluation parameters
        var res = engine
            .SetValue("resource", resource)
            .SetValue("participantRedemptions", participantRedemptions)
            .SetValue("teamRedemptions", teamRedemptions)
            .SetValue("teamSize", teamSize)
            .SetValue("totalRedemptions", resource.Redemptions?.Count ?? 0)
            .SetValue("hasTeam", participant?.TeamId.HasValue == true)
            .Evaluate(resource.RedemptionStmt)
            .ToObject();

        if (res is not bool)
        {
            throw new InvalidOperationException("Redemption script did not return a boolean.");
        }

        if (res is false)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var redemption = new ResourceRedemption
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            ActivityId = req.ActivityId,
            UserId = req.ParticipantUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(redemption).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                RedemptionId = redemption.Id,
                ResourceId = redemption.ResourceId,
                ActivityId = redemption.ActivityId,
                CreatedAt = redemption.CreatedAt.AssumeStoredAsUtc(),
            },
            ct
        );
    }
}
