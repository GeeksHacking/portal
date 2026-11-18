using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.Upload;

public class Endpoint(ISqlSugarClient sql, HeaderImageStorage storage, ILogger<Endpoint> logger)
    : Endpoint<Request, Response>
{
    private const long MaxBytes = 5 * 1024 * 1024;

    public override void Configure()
    {
        Put("admin/activities/{ActivityId:guid}/header-image");
        Policies(PolicyNames.Root);
        AllowFileUploads();
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Upload an activity header image";
            s.Description =
                "Uploads (or replaces) the header image of a hackathon, hackathon workshop or standalone workshop. "
                + "Accepts JPEG, PNG or WebP up to 5 MB.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (req.File is null || req.File.Length == 0)
        {
            AddError(r => r.File, "An image file is required.");
        }
        else if (req.File.Length > MaxBytes)
        {
            AddError(r => r.File, "Image must be 5 MB or smaller.");
        }

        ThrowIfAnyErrors();

        await using var stream = new MemoryStream();
        await req.File!.CopyToAsync(stream, ct);
        var format = DetectFormat(stream.GetBuffer().AsSpan(0, (int)stream.Length));
        if (format is null)
        {
            AddError(r => r.File, "Image must be a JPEG, PNG or WebP file.");
            ThrowIfAnyErrors();
        }

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

        stream.Position = 0;
        var newKey = await storage.UploadAsync(
            activity.Id,
            activity.Kind,
            stream,
            format!.Value.ContentType,
            format.Value.Extension,
            ct
        );

        var oldKey = activity.HeaderImageKey;
        await sql.Updateable<Activity>()
            .SetColumns(a => new Activity { HeaderImageKey = newKey, UpdatedAt = DateTimeOffset.UtcNow })
            .Where(a => a.Id == activity.Id)
            .ExecuteCommandAsync(ct);

        if (!string.IsNullOrEmpty(oldKey))
        {
            try
            {
                await storage.DeleteAsync(oldKey, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete previous header image {Key}", oldKey);
            }
        }

        await Send.OkAsync(new Response { HeaderImageUrl = storage.ToUrl(newKey)! }, ct);
    }

    private static (string ContentType, string Extension)? DetectFormat(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return ("image/jpeg", ".jpg");
        }

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return ("image/png", ".png");
        }

        if (
            bytes.Length >= 12
            && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
        )
        {
            return ("image/webp", ".webp");
        }

        return null;
    }
}
