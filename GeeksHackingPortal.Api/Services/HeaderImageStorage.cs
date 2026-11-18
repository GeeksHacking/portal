using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Options;
using Microsoft.Extensions.Options;

namespace GeeksHackingPortal.Api.Services;

/// <summary>
/// Stores activity header images in an S3-compatible bucket (Cloudflare R2, or RustFS locally).
/// Browsers upload directly to the bucket with presigned PUT URLs; the API only signs, verifies and deletes.
/// </summary>
public sealed class HeaderImageStorage : IDisposable
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public static readonly TimeSpan UploadUrlLifetime = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, string> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly R2Options _options;
    private readonly AmazonS3Client? _client;
    private readonly Protocol _protocol = Protocol.HTTPS;

    public HeaderImageStorage(IOptions<R2Options> options)
    {
        _options = options.Value;
        var (accessKeyId, secretAccessKey) = _options.ResolveCredentials();
        IsConfigured =
            (!string.IsNullOrWhiteSpace(_options.ServiceUrl) || !string.IsNullOrWhiteSpace(_options.AccountId))
            && !string.IsNullOrWhiteSpace(accessKeyId)
            && !string.IsNullOrWhiteSpace(secretAccessKey)
            && !string.IsNullOrWhiteSpace(_options.BucketName)
            && !string.IsNullOrWhiteSpace(_options.PublicBaseUrl);

        if (!IsConfigured)
        {
            return;
        }

        var serviceUrl = string.IsNullOrWhiteSpace(_options.ServiceUrl)
            ? $"https://{_options.AccountId}.r2.cloudflarestorage.com"
            : _options.ServiceUrl;
        var useHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        _protocol = useHttp ? Protocol.HTTP : Protocol.HTTPS;

        _client = new AmazonS3Client(
            new BasicAWSCredentials(accessKeyId, secretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                UseHttp = useHttp,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            }
        );
    }

    public bool IsConfigured { get; }

    public static bool IsAllowedContentType(string? contentType) =>
        contentType is not null && ExtensionsByContentType.ContainsKey(contentType);

    /// <summary>Key prefix that every header image of the activity lives under.</summary>
    public static string KeyPrefix(Guid activityId, ActivityKind kind) =>
        $"{(kind == ActivityKind.Hackathon ? "hackathon" : "workshops")}/{activityId}/header-";

    /// <summary>
    /// Creates a new object key and a presigned PUT URL for it. The browser must send the same
    /// <c>Content-Type</c> header, since it is part of the signature.
    /// </summary>
    public async Task<(string Key, string UploadUrl, DateTimeOffset ExpiresAt)> CreateUploadUrlAsync(
        Guid activityId,
        ActivityKind kind,
        string contentType
    )
    {
        var key = $"{KeyPrefix(activityId, kind)}{Guid.NewGuid():N}{ExtensionsByContentType[contentType]}";
        var expiresAt = DateTimeOffset.UtcNow.Add(UploadUrlLifetime);
        var url = await Client.GetPreSignedURLAsync(
            new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                ContentType = contentType.ToLowerInvariant(),
                Expires = expiresAt.UtcDateTime,
                Protocol = _protocol,
            }
        );
        return (key, url, expiresAt);
    }

    /// <summary>Returns the size of an uploaded object, or null if it does not exist.</summary>
    public async Task<long?> GetSizeAsync(string key, CancellationToken ct)
    {
        try
        {
            var metadata = await Client.GetObjectMetadataAsync(_options.BucketName, key, ct);
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Reads the first <paramref name="count"/> bytes of an object (for file signature checks).</summary>
    public async Task<byte[]> ReadHeadAsync(string key, int count, CancellationToken ct)
    {
        using var response = await Client.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                ByteRange = new ByteRange(0, count - 1),
            },
            ct
        );
        var buffer = new byte[count];
        var read = await response.ResponseStream.ReadAtLeastAsync(buffer, count, throwOnEndOfStream: false, ct);
        return buffer[..read];
    }

    public Task DeleteAsync(string key, CancellationToken ct) =>
        Client.DeleteObjectAsync(_options.BucketName, key, ct);

    /// <summary>Local development: creates the bucket and makes its objects publicly readable.</summary>
    public async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(Client, _options.BucketName))
        {
            await Client.PutBucketAsync(_options.BucketName, ct);
        }

        await Client.PutBucketPolicyAsync(
            new PutBucketPolicyRequest
            {
                BucketName = _options.BucketName,
                Policy = $$"""
                    {
                      "Version": "2012-10-17",
                      "Statement": [{
                        "Effect": "Allow",
                        "Principal": { "AWS": ["*"] },
                        "Action": ["s3:GetObject"],
                        "Resource": ["arn:aws:s3:::{{_options.BucketName}}/*"]
                      }]
                    }
                    """,
            },
            ct
        );
    }

    public string? ToUrl(string? key) => ToUrl(key, _options.PublicBaseUrl);

    public static string? ToUrl(string? key, string publicBaseUrl) =>
        string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(publicBaseUrl) ? null : $"{publicBaseUrl.TrimEnd('/')}/{key}";

    private AmazonS3Client Client =>
        _client ?? throw new InvalidOperationException("Header image storage is not configured.");

    public void Dispose() => _client?.Dispose();
}

/// <summary>Runs <see cref="HeaderImageStorage.EnsureBucketAsync"/> on startup when <c>R2:EnsureBucket</c> is set.</summary>
public sealed class HeaderImageBucketInitializer(
    HeaderImageStorage storage,
    IOptions<R2Options> options,
    ILogger<HeaderImageBucketInitializer> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.EnsureBucket || !storage.IsConfigured)
        {
            return;
        }

        try
        {
            await storage.EnsureBucketAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set up header image bucket {Bucket}", options.Value.BucketName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
