using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.UploadUrl;

public class Endpoint(ISqlSugarClient sql, HeaderImageStorage storage) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("admin/activities/{ActivityId:guid}/header-image/upload-url");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Create a header image upload URL";
            s.Description =
                "Returns a short-lived presigned URL the browser uploads the image to directly. "
                + "PUT the file to it with the same Content-Type, then call PUT /admin/activities/{activityId}/header-image "
                + "with the returned key. Accepts JPEG, PNG or WebP up to 5 MB.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!HeaderImageStorage.IsAllowedContentType(req.ContentType))
        {
            AddError(r => r.ContentType, "Image must be a JPEG, PNG or WebP file.");
        }

        if (req.ContentLength <= 0)
        {
            AddError(r => r.ContentLength, "An image file is required.");
        }
        else if (req.ContentLength > HeaderImageStorage.MaxBytes)
        {
            AddError(r => r.ContentLength, "Image must be 5 MB or smaller.");
        }

        ThrowIfAnyErrors();

        var activity = await sql.Queryable<Activity>().InSingleAsync(req.ActivityId);
        if (activity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!storage.IsConfigured)
        {
            AddError("Header image storage is not configured.");
            await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
            return;
        }

        var (key, uploadUrl, expiresAt) = await storage.CreateUploadUrlAsync(activity.Id, activity.Kind, req.ContentType);
        await Send.OkAsync(new Response { UploadUrl = uploadUrl, Key = key, ExpiresAt = expiresAt }, ct);
    }
}
