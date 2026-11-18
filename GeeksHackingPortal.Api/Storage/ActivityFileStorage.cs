using Amazon.S3;
using Amazon.S3.Model;
using GeeksHackingPortal.Api.Entities;
using Microsoft.Net.Http.Headers;

namespace GeeksHackingPortal.Api.Storage;

/// <summary>
/// Stores <see cref="ActivityFile"/> contents in the S3-compatible bucket and hands out download URLs.
/// </summary>
/// <remarks>
/// Downloads use presigned URLs so clients fetch bytes straight from the bucket instead of through the API.
/// The client is created on first use, so the API still starts (and pages without files still load) when
/// object storage is not configured.
/// </remarks>
public sealed class ActivityFileStorage(S3StorageSettings settings, ILogger<ActivityFileStorage> logger)
{
    public const long MaxBannerBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Stays below Cloud Run's 32 MiB request limit, uploads go through the API.
    /// </summary>
    public const long MaxAttachmentBytes = 25 * 1024 * 1024;

    public const int MaxFileNameLength = 255;

    private readonly Lazy<IAmazonS3> _s3 = new(() => S3Storage.CreateClient(settings));

    public bool IsConfigured => settings.IsConfigured;

    public Task EnsureBucketAsync(CancellationToken ct) =>
        S3Storage.EnsureBucketAsync(_s3.Value, settings, ct);

    /// <summary>
    /// Uploads the file under a new key and returns the (not yet saved) entity describing it.
    /// </summary>
    public async Task<ActivityFile> UploadAsync(
        Guid activityId,
        ActivityFileKind kind,
        IFormFile upload,
        string contentType,
        Guid uploadedByUserId,
        CancellationToken ct
    )
    {
        var fileName = Path.GetFileName(upload.FileName);
        var folder = kind == ActivityFileKind.Banner ? "banners" : "files";
        var key = $"activities/{activityId:N}/{folder}/{Guid.NewGuid():N}{Extension(fileName)}";

        await using var stream = upload.OpenReadStream();
        await _s3.Value.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = settings.Bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false,
            },
            ct
        );

        return new ActivityFile
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            Kind = kind,
            FileName = fileName,
            StorageKey = key,
            ContentType = contentType,
            SizeBytes = upload.Length,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Best-effort removal of stored objects; failures are logged, never thrown, so database changes that
    /// already committed are not reported as failed.
    /// </summary>
    public async Task DeleteObjectsAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            try
            {
                await _s3.Value.DeleteObjectAsync(
                    new DeleteObjectRequest { BucketName = settings.Bucket, Key = key },
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete activity file object {Key}", key);
            }
        }
    }

    /// <summary>
    /// A time-limited URL to fetch the file. Banners are served inline for use in image tags; attachments are
    /// always served as downloads under their original name.
    /// </summary>
    public string GetDownloadUrl(ActivityFile file)
    {
        var disposition = new ContentDispositionHeaderValue(
            file.Kind == ActivityFileKind.Banner ? "inline" : "attachment"
        );
        disposition.SetHttpFileName(file.FileName);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = settings.Bucket,
            Key = file.StorageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(settings.DownloadUrlLifetime),
            Protocol = settings.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? Protocol.HTTP
                : Protocol.HTTPS,
        };
        request.ResponseHeaderOverrides.ContentDisposition = disposition.ToString();

        return _s3.Value.GetPreSignedURL(request);
    }

    /// <summary>
    /// Returns why an upload is unacceptable, or null when it can be stored.
    /// </summary>
    public static string? ValidateUpload(IFormFile? upload, long maxBytes)
    {
        if (upload is null || upload.Length == 0)
            return "A file is required.";
        if (upload.Length > maxBytes)
            return $"Files must be {maxBytes / (1024 * 1024)} MB or smaller.";

        var fileName = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
            return "The file must have a name.";
        if (fileName.Length > MaxFileNameLength)
            return $"File names must be {MaxFileNameLength} characters or fewer.";

        return null;
    }

    /// <summary>
    /// Returns the content type of a PNG, JPEG, GIF or WebP image from its leading bytes, or null for anything
    /// else. Banners are identified by their contents rather than the type the client declared.
    /// </summary>
    public static async Task<string?> DetectBannerContentTypeAsync(IFormFile upload, CancellationToken ct)
    {
        var header = new byte[12];
        await using var stream = upload.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        var bytes = header.AsSpan(0, read);

        if (bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return "image/png";
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
            return "image/jpeg";
        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8))
            return "image/gif";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }

    private static string Extension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var cleaned = new string(extension.TrimStart('.').Where(char.IsAsciiLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(cleaned) ? "" : $".{cleaned.ToLowerInvariant()}";
    }
}
