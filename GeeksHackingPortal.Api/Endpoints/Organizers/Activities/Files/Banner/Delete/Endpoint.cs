using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Banner.Delete;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete(ActivityFileRouteRequest.Routes("organizers", "banner"));
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "Remove an activity banner";
            s.Description = "Removes the banner of a hackathon, hackathon workshop or standalone workshop, if it has one.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var activity = await req.FindActivityAsync(sql, ct);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var banners = await sql.Queryable<ActivityFile>()
            .Where(f => f.ActivityId == activity.Id && f.Kind == ActivityFileKind.Banner)
            .ToListAsync(ct);
        if (banners.Count > 0)
        {
            await sql.Deleteable(banners).ExecuteCommandAsync(ct);
            await storage.DeleteObjectsAsync(banners.Select(f => f.StorageKey));
        }

        await Send.NoContentAsync(ct);
    }
}
