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
            .Where(h => h.ShortCode == req.ShortCode)
            .Includes(h => h.Activity)
            .FirstAsync(ct);

        if (hackathon is null || !hackathon.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existing = await sql.Queryable<Participant>()
            .Where(p => p.HackathonId == hackathon.Id && p.UserId == userId.Value)
            .FirstAsync(ct);

        if (existing is null)
        {
            var now = DateTimeOffset.UtcNow;
            var participantId = Guid.NewGuid();
            existing = new Participant
            {
                Id = participantId,
                HackathonId = hackathon.Id,
                UserId = userId.Value,
                TeamId = null,
                JoinedAt = now,
            };

            var registration = new ActivityRegistration
            {
                Id = participantId,
                ActivityId = hackathon.ActivityId,
                UserId = userId.Value,
                Status = ActivityRegistrationStatus.Registered,
                RegisteredAt = now,
            };

            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Insertable(existing).ExecuteCommandAsync(ct);
                await sql.Insertable(registration).ExecuteCommandAsync(ct);
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }
        }
        else if (existing.WithdrawnAt is not null)
        {
            existing.WithdrawnAt = null;
            var registration = await sql.Queryable<ActivityRegistration>()
                .InSingleAsync(existing.Id);

            if (registration is null)
            {
                registration = new ActivityRegistration
                {
                    Id = existing.Id,
                    ActivityId = hackathon.ActivityId,
                    UserId = userId.Value,
                    RegisteredAt = existing.JoinedAt,
                };
            }

            registration.Status = ActivityRegistrationStatus.Registered;
            registration.WithdrawnAt = null;

            var transactionResult = await sql.Ado.UseTranAsync(async () =>
            {
                await sql.Updateable(existing).ExecuteCommandAsync(ct);
                await sql.Storageable(registration).ExecuteCommandAsync();
            });

            if (!transactionResult.IsSuccess)
            {
                throw transactionResult.ErrorException!;
            }
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
