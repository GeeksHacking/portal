using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Organizers.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("organizers/standalone-workshops/{StandaloneWorkshopId:guid}/organizers");
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

        var organizers = await sql.Queryable<ActivityOrganizer>()
            .InnerJoin<User>((o, u) => o.UserId == u.Id)
            .Where(o => o.ActivityId == req.StandaloneWorkshopId)
            .Select(
                (o, u) =>
                    new Response.OrganizerItem
                    {
                        UserId = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = u.Email,
                        Type = o.Type.ToString(),
                    }
            )
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Organizers = organizers }, ct);
    }
}
