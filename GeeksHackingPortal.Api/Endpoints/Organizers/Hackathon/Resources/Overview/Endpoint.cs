using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Overview;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/hackathons/{HackathonId:guid}/resources/{ResourceId:guid}/overview");
        Policies(PolicyNames.OrganizerForHackathon);
        Description(b => b.WithTags("Organizers", "Resources"));
        Summary(s =>
        {
            s.Summary = "Get resource redemption overview";
            s.Description =
                "Returns participant redemption status and recent redemption activity for a resource.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var resource = await sql.Queryable<Resource>()
            .Where(r => r.Id == req.ResourceId && r.HackathonId == req.HackathonId)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var participants = await sql.Queryable<Participant>()
            .LeftJoin<User>((p, u) => p.UserId == u.Id)
            .Where((p, u) => p.HackathonId == req.HackathonId)
            .Select((p, u) => new { Participant = p, User = u })
            .ToListAsync(ct);

        var participantIds = participants.Select(p => p.Participant.Id).ToList();
        var userIdByParticipantId = participants.ToDictionary(p => p.Participant.Id, p => p.Participant.UserId);
        var participantIdByUserId = participants.ToDictionary(p => p.Participant.UserId, p => p.Participant.Id);
        var userNameByParticipantId = participants.ToDictionary(
            p => p.Participant.Id,
            p => $"{p.User.FirstName} {p.User.LastName}".Trim()
        );

        var redemptions = await sql.Queryable<ResourceRedemption>()
            .Where(r =>
                r.HackathonId == req.HackathonId
                && r.ResourceId == req.ResourceId
                && userIdByParticipantId.Values.Contains(r.RedeemerId)
            )
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var redemptionsByUserId = redemptions
            .GroupBy(r => r.RedeemerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).ToList());

        var participantDtos = participants
            .Select(p =>
            {
                var participantRedemptions =
                    redemptionsByUserId.GetValueOrDefault(p.Participant.UserId) ?? [];

                return new ParticipantResourceRedemptionDto
                {
                    ParticipantId = p.Participant.Id,
                    UserId = p.Participant.UserId,
                    UserName = userNameByParticipantId[p.Participant.Id],
                    HasRedeemed = participantRedemptions.Count > 0,
                    RedemptionCount = participantRedemptions.Count,
                    LastRedeemedAt = participantRedemptions.FirstOrDefault()?.CreatedAt.AssumeStoredAsUtc(),
                };
            })
            .OrderByDescending(p => p.LastRedeemedAt.HasValue)
            .ThenByDescending(p => p.LastRedeemedAt)
            .ThenBy(p => p.UserName)
            .ToList();

        var auditTrail = redemptions
            .Select(r =>
            {
                var participantId = participantIdByUserId.GetValueOrDefault(r.RedeemerId);
                return new ResourceAuditTrailItemDto
                {
                    RedemptionId = r.Id,
                    ParticipantId = participantId,
                    UserId = r.RedeemerId,
                    UserName = userNameByParticipantId.GetValueOrDefault(participantId, "Unknown"),
                    Timestamp = r.CreatedAt.AssumeStoredAsUtc(),
                };
            })
            .Take(50)
            .ToList();

        await Send.OkAsync(
            new Response
            {
                ResourceId = resource.Id,
                ResourceName = resource.Name,
                IsPublished = resource.IsPublished,
                TotalRedemptions = redemptions.Count,
                UniqueRedeemers = redemptionsByUserId.Count,
                Participants = participantDtos,
                AuditTrail = auditTrail,
            },
            ct
        );
    }
}
