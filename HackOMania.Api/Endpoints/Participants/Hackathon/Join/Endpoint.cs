using FastEndpoints;
using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Participants.Hackathon.Join;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/{Id}/join");
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Join a hackathon";
            s.Description = "Registers the current user as a participant in the hackathon.";
        });
        // Rate limit: 10 join attempts per minute per user
        Throttle(hitLimit: 10, durationSeconds: 60);
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

        var userId = User.GetUserId<Guid>();
        var existing = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == userId)
            .FirstAsync(ct);

        if (existing is null)
        {
            existing = new Participant
            {
                HackathonId = hackathon.Id,
                UserId = userId,
                TeamId = null,
                JoinedAt = DateTimeOffset.UtcNow,
            };

            await sql.Insertable(existing).ExecuteCommandAsync(ct);
        }

        await Send.OkAsync(
            new Response
            {
                HackathonId = hackathon.Id,
                UserId = userId,
                JoinedAt = existing.JoinedAt,
            },
            ct
        );
    }
}
