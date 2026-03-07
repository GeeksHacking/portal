using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using Jint;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Resources.Redeem;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
            "organizers/hackathons/{HackathonId:guid}/participants/{ParticipantUserId:guid}/resources/{ResourceId}/redemptions"
        );
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Resources").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Redeem a resource for a participant";
            s.Description =
                "Creates a redemption record for a resource in the hackathon on behalf of a participant.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = await sql.Queryable<Resource>()
            .Includes(r => r.Redemptions)
            .Where(r => r.Id == req.ResourceId && r.HackathonId == hackathon.Id && r.IsPublished)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Get participant and team information
        var participant = await sql.Queryable<Participant>()
            .Includes(p => p.Team, t => t.Members)
            .FirstAsync(
                p => p.UserId == req.ParticipantUserId && p.HackathonId == hackathon.Id,
                ct
            );

        if (participant is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Count redemptions by this participant
        var participantRedemptions =
            resource.Redemptions?.Count(r => r.RedeemerId == req.ParticipantUserId) ?? 0;

        // Count redemptions by this participant's team (if they have one)
        int teamRedemptions = 0;
        int teamSize = 0;
        if (participant.TeamId.HasValue && participant.Team is not null)
        {
            var teamMemberIds =
                participant.Team.Members?.Select(m => m.UserId).ToList() ?? new List<Guid>();
            teamRedemptions =
                resource.Redemptions?.Count(r => teamMemberIds.Contains(r.RedeemerId)) ?? 0;
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
            .SetValue("hasTeam", participant.TeamId.HasValue)
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
            HackathonId = hackathon.Id,
            RedeemerId = req.ParticipantUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await sql.Insertable(redemption).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                RedemptionId = redemption.Id,
                ResourceId = redemption.ResourceId,
                HackathonId = redemption.HackathonId,
                CreatedAt = redemption.CreatedAt,
            },
            ct
        );
    }
}
