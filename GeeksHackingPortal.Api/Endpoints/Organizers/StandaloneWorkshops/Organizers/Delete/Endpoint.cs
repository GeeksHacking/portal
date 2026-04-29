using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.Delete;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/organizers/{UserId:guid}");
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

        var currentUserId = User.GetUserId();
        if (currentUserId is null || currentUserId.Value == req.UserId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var currentOrganizer = await sql.Queryable<ActivityOrganizer>()
            .Where(o => o.ActivityId == req.StandaloneWorkshopId && o.UserId == currentUserId.Value)
            .FirstAsync(ct);
        if (currentOrganizer?.Type != OrganizerType.Admin)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var deleted = await sql.Deleteable<ActivityOrganizer>()
            .Where(o => o.UserId == req.UserId && o.ActivityId == req.StandaloneWorkshopId)
            .ExecuteCommandAsync(ct);

        if (deleted == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
