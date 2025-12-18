using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.Redeem;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{Id}/resources/{ResourceId}/redemptions");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Resources"));
        Summary(s =>
        {
            s.Summary = "Redeem a resource";
            s.Description = "Creates a redemption record for a resource in the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Id))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var hackathon = await membership.FindHackathon(req.Id, ct);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = await sql.Queryable<Resource>()
            .Where(r => r.Id.ToString() == req.ResourceId && r.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var redemption = new ResourceRedemption
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            HackathonId = hackathon.Id,
            RedeemerId = User.GetUserId<Guid>(),
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
