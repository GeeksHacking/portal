using System.Net.Http.Headers;
using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Features.ActivityFiles;
using GeeksHackingPortal.Api.Storage;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Create;

public class Endpoint(ISqlSugarClient sql, ActivityFileStorage storage)
    : Endpoint<Request, ActivityFileResponse>
{
    public override void Configure()
    {
        Post(ActivityFileRouteRequest.Routes("organizers", "files"));
        Policies(PolicyNames.OrganizerForActivity);
        AllowFileUploads();
        MaxRequestBodySize(ActivityFileStorage.MaxAttachmentBytes + 64 * 1024);
        Description(b => b.WithTags("Files"));
        Summary(s =>
        {
            s.Summary = "Attach a file to an activity";
            s.Description =
                "Uploads a file (up to 25 MB) and attaches it to a hackathon, hackathon workshop or standalone workshop. Once the activity is published, anyone can list and download its files.";
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

        var uploadError = ActivityFileStorage.ValidateUpload(req.File, ActivityFileStorage.MaxAttachmentBytes);
        if (uploadError is not null)
        {
            AddError(r => r.File, uploadError);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        // Attachments are always downloaded rather than rendered, so the declared type is kept as a hint only.
        var contentType = MediaTypeHeaderValue.TryParse(req.File.ContentType, out var mediaType)
            ? mediaType.ToString()
            : "application/octet-stream";

        var file = await storage.UploadAsync(
            activity.Id,
            ActivityFileKind.Attachment,
            req.File,
            contentType,
            User.GetUserId()!.Value,
            ct
        );

        try
        {
            await sql.Insertable(file).ExecuteCommandAsync(ct);
        }
        catch
        {
            await storage.DeleteObjectsAsync([file.StorageKey]);
            throw;
        }

        await Send.ResponseAsync(
            ActivityFileResponse.From(file, storage),
            StatusCodes.Status201Created,
            ct
        );
    }
}
