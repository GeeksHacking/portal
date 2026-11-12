using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.List;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get(ActivityFileRouteRequest.Routes("organizers", "files"));
        Policies(PolicyNames.OrganizerForActivity);
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "List activity files";
            s.Description =
                "Lists the banner and attached files of a hackathon, hackathon workshop or standalone workshop, with time-limited download URLs.";
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

        var files = await sql.Queryable<ActivityFile>()
            .Where(f => f.ActivityId == activity.Id)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);

        var banner = files.Where(f => f.Kind == ActivityFileKind.Banner).MaxBy(f => f.CreatedAt);

        await Send.OkAsync(
            new Response
            {
                Banner = banner is null ? null : ActivityFileResponse.From(banner, storage),
                Files =
                [
                    .. files
                        .Where(f => f.Kind == ActivityFileKind.Attachment)
                        .Select(f => ActivityFileResponse.From(f, storage)),
                ],
            },
            ct
        );
    }
}
