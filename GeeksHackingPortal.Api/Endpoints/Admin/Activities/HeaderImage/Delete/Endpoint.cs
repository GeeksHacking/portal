using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.Delete;

public class Endpoint(ISqlSugarClient sql, HeaderImageStorage storage, ILogger<Endpoint> logger)
    : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("admin/activities/{ActivityId:guid}/header-image");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Remove an activity header image";
            s.Description = "Removes the header image of a hackathon, hackathon workshop or standalone workshop.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var activity = await sql.Queryable<Activity>().InSingleAsync(req.ActivityId);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var oldKey = activity.HeaderImageKey;
        if (string.IsNullOrEmpty(oldKey))
        {
            await Send.NoContentAsync(ct);
            return;
        }

        await sql.Updateable<Activity>()
            .SetColumns(a => new Activity { HeaderImageKey = null, UpdatedAt = DateTimeOffset.UtcNow })
            .Where(a => a.Id == activity.Id)
            .ExecuteCommandAsync(ct);

        if (storage.IsConfigured)
        {
            try
            {
                await storage.DeleteAsync(oldKey, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete header image {Key}", oldKey);
            }
        }

        await Send.NoContentAsync(ct);
    }
}
