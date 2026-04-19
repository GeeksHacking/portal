using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.JoinByShortCode;

public class Endpoint(ISqlSugarClient sql, IWebHostEnvironment env) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/hackathons/join");
        Description(b => b.WithTags("Participants", "Hackathons"));
        Summary(s =>
        {
            s.Summary = "Join a hackathon by short code";
            s.Description =
                "Registers the current user as a participant in the hackathon using the hackathon's short code.";
        });

        // The join endpoint is hit heavily by the integration test suite.
        // Throttling is valuable in production, but it makes parallel tests flaky.
        if (env.IsProduction())
        {
            Throttle(hitLimit: 10, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var hackathon = await sql.Queryable<Entities.Hackathon>()
            .Where(h => h.ShortCode == req.ShortCode && h.IsPublished)
            .FirstAsync(ct);

        if (hackathon is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existing = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == userId.Value)
            .FirstAsync(ct);

        if (existing is null)
        {
            existing = new Participant
            {
                HackathonId = hackathon.Id,
                UserId = userId.Value,
                TeamId = null,
                JoinedAt = DateTimeOffset.UtcNow,
            };

            await sql.Insertable(existing).ExecuteCommandAsync(ct);
        }
        else if (existing.WithdrawnAt is not null)
        {
            existing.WithdrawnAt = null;
            await sql.Updateable(existing).ExecuteCommandAsync(ct);
        }

        await Send.OkAsync(
            new Response
            {
                HackathonId = hackathon.Id,
                UserId = userId.Value,
                JoinedAt = existing.JoinedAt,
            },
            ct
        );
    }
}
