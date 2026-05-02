using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Update;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch(
            "organizers/hackathons/{ActivityId:guid}/resources/{ResourceId:guid}",
            "organizers/standalone-workshops/{ActivityId:guid}/resources/{ResourceId:guid}"
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

        var resource = await sql.Queryable<Resource>()
            .Where(r => r.Id == req.ResourceId && r.ActivityId == req.ActivityId)
            .FirstAsync(ct);

        if (resource is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            resource.Name = req.Name;
        }

        if (req.Description is not null)
        {
            resource.Description = req.Description;
        }

        if (req.RedemptionStmt is not null)
        {
            resource.RedemptionStmt = req.RedemptionStmt;
        }

        if (req.IsPublished.HasValue)
        {
            resource.IsPublished = req.IsPublished.Value;
        }

        await sql.Updateable(resource).ExecuteCommandAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = resource.Id,
                ActivityId = resource.ActivityId,
                Name = resource.Name,
                Description = resource.Description,
                RedemptionStmt = resource.RedemptionStmt,
                IsPublished = resource.IsPublished,
            },
            ct
        );
    }
}
