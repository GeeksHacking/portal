using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Status;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("participants/standalone-workshops/{StandaloneWorkshopId:guid}/status");
        Description(b => b.WithTags("Participants", "Standalone Workshops").Accepts<Request>());
        Summary(s =>
        {
            s.Summary = "Get my standalone workshop status";
            s.Description = "Returns the current user's registration status for a standalone workshop.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var workshop = await sql.Queryable<StandaloneWorkshop>()
            .Includes(w => w.Activity)
            .WithCache()
            .InSingleAsync(req.StandaloneWorkshopId);
        if (workshop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var isOrganizer = await sql.Queryable<ActivityOrganizer>()
            .WithCache()
            .AnyAsync(o => o.ActivityId == workshop.ActivityId && o.UserId == userId.Value, ct);
        var registration = await sql.Queryable<ActivityRegistration>()
            .Where(r => r.ActivityId == workshop.ActivityId && r.UserId == userId.Value)
            .WithCache()
            .FirstAsync(ct);

        await Send.OkAsync(
            new Response
            {
                IsRegistered =
                    registration is
                    {
                        Status: ActivityRegistrationStatus.Registered,
                        WithdrawnAt: null,
                    },
                IsOrganizer = isOrganizer,
                RegisteredAt = registration?.RegisteredAt,
                WithdrawnAt = registration?.WithdrawnAt,
            },
            ct
        );
    }
}
