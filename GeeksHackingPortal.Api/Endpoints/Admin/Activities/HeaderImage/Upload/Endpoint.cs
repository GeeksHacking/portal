using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.Activities.HeaderImage.Upload;

public class Endpoint(ISqlSugarClient sql, HeaderImageStorage storage, ILogger<Endpoint> logger)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("admin/activities/{ActivityId:guid}/header-image");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Set an activity header image";
            s.Description =
                "Sets (or replaces) the header image of a hackathon, hackathon workshop or standalone workshop "
                + "to an image already uploaded through POST /admin/activities/{activityId}/header-image/upload-url. "
                + "The uploaded object must be a JPEG, PNG or WebP up to 5 MB, otherwise it is deleted.";
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

        if (!storage.IsConfigured)
        {
            AddError("Header image storage is not configured.");
            await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
            return;
        }

        // Only accept keys this activity's upload URLs could have produced.
        var prefix = HeaderImageStorage.KeyPrefix(activity.Id, activity.Kind);
        if (
            string.IsNullOrEmpty(req.Key)
            || !req.Key.StartsWith(prefix, StringComparison.Ordinal)
            || req.Key.AsSpan(prefix.Length).IndexOfAny('/', '\\') >= 0
            || req.Key == activity.HeaderImageKey
        )
        {
            AddError(r => r.Key, "Unknown upload key.");
            ThrowIfAnyErrors();
        }

        var size = await storage.GetSizeAsync(req.Key, ct);
        if (size is null)
        {
            AddError(r => r.Key, "The image has not been uploaded.");
            ThrowIfAnyErrors();
        }

        string? problem = null;
        if (size is 0 or > HeaderImageStorage.MaxBytes)
        {
            problem = "Image must be 5 MB or smaller.";
        }
        else if (!MatchesExtension(req.Key, await storage.ReadHeadAsync(req.Key, 12, ct)))
        {
            problem = "Image must be a JPEG, PNG or WebP file.";
        }

        if (problem is not null)
        {
            await TryDeleteAsync(req.Key, ct);
            AddError(r => r.Key, problem);
            ThrowIfAnyErrors();
        }

        var oldKey = activity.HeaderImageKey;
        await sql.Updateable<Activity>()
            .SetColumns(a => new Activity { HeaderImageKey = req.Key, UpdatedAt = DateTimeOffset.UtcNow })
            .Where(a => a.Id == activity.Id)
            .ExecuteCommandAsync(ct);

        if (!string.IsNullOrEmpty(oldKey))
        {
            await TryDeleteAsync(oldKey, ct);
        }

        await Send.OkAsync(new Response { HeaderImageUrl = storage.ToUrl(req.Key)! }, ct);
    }

    private async Task TryDeleteAsync(string key, CancellationToken ct)
    {
        try
        {
            await storage.DeleteAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete header image {Key}", key);
        }
    }

    /// <summary>Checks the file signature matches the extension chosen from the declared content type.</summary>
    private static bool MatchesExtension(string key, ReadOnlySpan<byte> bytes) =>
        Path.GetExtension(key) switch
        {
            ".jpg" => bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
            ".png" => bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };
}
