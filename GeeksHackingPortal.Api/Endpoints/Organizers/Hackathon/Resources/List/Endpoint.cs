using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.List;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(
            "organizers/hackathons/{ActivityId:guid}/resources",
            "organizers/standalone-workshops/{ActivityId:guid}/resources"
        );
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Resources"));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var activityExists = await sql.Queryable<Activity>().AnyAsync(a => a.Id == req.ActivityId, ct);
        if (!activityExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var resources = await sql.Queryable<Resource>()
            .Where(r => r.ActivityId == req.ActivityId)
            .Select(r => new Response.ResponseResource
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RedemptionStmt = r.RedemptionStmt,
                IsPublished = r.IsPublished,
            })
            
            .ToListAsync(ct);

        await Send.OkAsync(new Response { Resources = resources }, ct);
    }
}
