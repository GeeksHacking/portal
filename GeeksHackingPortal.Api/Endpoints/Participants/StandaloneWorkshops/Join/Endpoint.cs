using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Join;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("participants/standalone-workshops/{StandaloneWorkshopId:guid}/join");
        Description(b => b.WithTags("Participants", "Standalone Workshops").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Join a standalone workshop";
            s.Description = "Registers the current user for a standalone workshop.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var workshop = await sql.Queryable<StandaloneWorkshop>()
            .Includes(w => w.Activity)
            .InSingleAsync(req.StandaloneWorkshopId);
        if (workshop is null || !workshop.IsPublished)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var registration = await sql.Queryable<ActivityRegistration>()
            .FirstAsync(r => r.ActivityId == workshop.ActivityId && r.UserId == userId.Value, ct);

        if (registration is null)
        {
            registration = new ActivityRegistration
            {
                Id = Guid.NewGuid(),
                ActivityId = workshop.ActivityId,
                UserId = userId.Value,
                Status = ActivityRegistrationStatus.Registered,
                RegisteredAt = DateTimeOffset.UtcNow,
            };

            await sql.Insertable(registration).ExecuteCommandAsync(ct);
        }
        else if (registration.Status == ActivityRegistrationStatus.Withdrawn || registration.WithdrawnAt is not null)
        {
            registration.Status = ActivityRegistrationStatus.Registered;
            registration.WithdrawnAt = null;
            await sql.Updateable(registration).ExecuteCommandAsync(ct);
        }

        await Send.OkAsync(
            new Response
            {
                StandaloneWorkshopId = workshop.Id,
                UserId = userId.Value,
                JoinedAt = registration.RegisteredAt,
            },
            ct
        );
    }
}
