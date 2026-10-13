using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Delete;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete(ActivityFileRouteRequest.Routes("organizers", "files/{fileId:guid}"));
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "Delete an activity file";
            s.Description = "Deletes a file attached to a hackathon, hackathon workshop or standalone workshop.";
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

        var file = await sql.Queryable<ActivityFile>()
            .Where(f =>
                f.Id == req.FileId
                && f.ActivityId == activity.Id
                && f.Kind == ActivityFileKind.Attachment
            )
            .FirstAsync(ct);
        if (file is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await sql.Deleteable(file).ExecuteCommandAsync(ct);
        await storage.DeleteObjectsAsync([file.StorageKey]);

        await Send.NoContentAsync(ct);
    }
}
