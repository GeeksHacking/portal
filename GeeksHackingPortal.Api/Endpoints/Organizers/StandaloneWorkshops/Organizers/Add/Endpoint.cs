using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.Add;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/organizers");
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Organizers", "Standalone Workshops"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var exists = await sql.Queryable<StandaloneWorkshop>()
            .AnyAsync(w => w.Id == req.StandaloneWorkshopId, ct);
        if (!exists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var targetUser = await sql.Queryable<User>().InSingleAsync(req.UserId);
        if (targetUser is null)
        {
            AddError(r => r.UserId, "User not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var organizerExists = await sql.Queryable<ActivityOrganizer>()
            .AnyAsync(
                o => o.UserId == req.UserId && o.ActivityId == req.StandaloneWorkshopId,
                ct
            );
        if (organizerExists)
        {
            AddError(r => r.UserId, "User is already an organizer for this standalone workshop.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await sql.Insertable(
                new ActivityOrganizer
                {
                    Id = Guid.NewGuid(),
                    ActivityId = req.StandaloneWorkshopId,
                    UserId = req.UserId,
                    Type = req.Type,
                    CreatedAt = DateTimeOffset.UtcNow,
                }
            )
            .ExecuteCommandAsync(ct);

        await Send.OkAsync(new Response { UserId = req.UserId, Type = req.Type }, ct);
    }
}
