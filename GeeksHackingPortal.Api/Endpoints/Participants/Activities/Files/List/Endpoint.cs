using FastEndpoints;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Activities.Files.List;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(ActivityFileRouteRequest.Routes("participants", "files"));
        AllowAnonymous();
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "List published activity files";
            s.Description =
                "Lists the files attached to a published hackathon, hackathon workshop or standalone workshop, with time-limited download URLs.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var activity = await req.FindActivityAsync(sql, ct);
        if (activity is null || !activity.IsPublished || !await IsHackathonPublishedAsync(req, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var files = await sql.Queryable<ActivityFile>()
            .Where(f => f.ActivityId == activity.Id && f.Kind == ActivityFileKind.Attachment)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

        await Send.OkAsync(
            new Response { Files = [.. files.Select(f => ActivityFileResponse.From(f, storage))] },
            ct
        );
    }

    /// <summary>
    /// A hackathon workshop is only public while its hackathon is.
    /// </summary>
    private async Task<bool> IsHackathonPublishedAsync(Request req, CancellationToken ct) =>
        req.WorkshopId is null
        || await sql.Queryable<Activity>().AnyAsync(a => a.Id == req.HackathonId && a.IsPublished, ct);
}
