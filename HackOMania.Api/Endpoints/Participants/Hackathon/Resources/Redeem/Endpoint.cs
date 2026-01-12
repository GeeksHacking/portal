using FastEndpoints;
using HackOMania.Api.Authorization;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using Jint;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Resources.Redeem;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{HackathonId:guid}/resources/{ResourceId}/redemptions");
        Policies(PolicyNames.ParticipantForHackathon);
        Description(b => b.WithTags("Participants", "Resources").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Redeem a resource";
            s.Description = "Creates a redemption record for a resource in the hackathon.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var hackathon = await sql.Queryable<Entities.Hackathon>().InSingleAsync(req.HackathonId);
        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resource = await sql.Queryable<Resource>()
            .Includes(r => r.Redemptions)
            .Where(r => r.Id == req.ResourceId && r.HackathonId == hackathon.Id)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var engine = new Engine(options =>
        {
            options.LimitMemory(4_000_000);
            options.TimeoutInterval(TimeSpan.FromSeconds(5));
            options.CancellationToken(ct);
        });

        var res = engine
            .SetValue("resource", resource)
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
            RedeemerId = userId.Value,
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
