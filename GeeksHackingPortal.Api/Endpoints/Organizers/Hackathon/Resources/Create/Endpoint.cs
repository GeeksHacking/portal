using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Resources.Create;

public class Endpoint(ISqlSugarClient sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post(
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

        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            ActivityId = req.ActivityId,
            Name = req.Name,
            Description = req.Description ?? string.Empty,
            RedemptionStmt = req.RedemptionStmt,
            IsPublished = req.IsPublished,
        };

        await sql.Insertable(resource).ExecuteCommandAsync(ct);

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
