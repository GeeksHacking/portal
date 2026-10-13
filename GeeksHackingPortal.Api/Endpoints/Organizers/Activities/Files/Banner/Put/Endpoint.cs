using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Banner.Put;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage)
    : Endpoint<Request, ActivityFileResponse>
{
    public override void Configure()
    {
        Put(ActivityFileRouteRequest.Routes("organizers", "banner"));
        Policies(PolicyNames.OrganizerForActivity);
        AllowFileUploads();
        MaxRequestBodySize(ActivityFileStorage.MaxBannerBytes + 64 * 1024);
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "Set an activity banner";
            s.Description =
                "Uploads a PNG, JPEG, GIF or WebP image (up to 10 MB) as the banner of a hackathon, hackathon workshop or standalone workshop, replacing any existing banner.";
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

        var uploadError = ActivityFileStorage.ValidateUpload(req.File, ActivityFileStorage.MaxBannerBytes);
        if (uploadError is not null)
        {
            AddError(r => r.File, uploadError);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var contentType = await ActivityFileStorage.DetectBannerContentTypeAsync(req.File, ct);
        if (contentType is null)
        {
            AddError(r => r.File, "Banners must be PNG, JPEG, GIF or WebP images.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var banner = await storage.UploadAsync(
            activity.Id,
            ActivityFileKind.Banner,
            req.File,
            contentType,
            User.GetUserId()!.Value,
            ct
        );

        List<ActivityFile> previousBanners;
        try
        {
            using var tran = sql.Ado.UseTran();

            previousBanners = await sql.Queryable<ActivityFile>()
                .Where(f => f.ActivityId == activity.Id && f.Kind == ActivityFileKind.Banner)
                .ToListAsync(ct);
            if (previousBanners.Count > 0)
            {
                await sql.Deleteable(previousBanners).ExecuteCommandAsync(ct);
            }

            await sql.Insertable(banner).ExecuteCommandAsync(ct);

            tran.CommitTran();
        }
        catch
        {
            await storage.DeleteObjectsAsync([banner.StorageKey]);
            throw;
        }

        await storage.DeleteObjectsAsync(previousBanners.Select(f => f.StorageKey));

        await Send.OkAsync(ActivityFileResponse.From(banner, storage), ct);
    }
}
