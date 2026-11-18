using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using GeeksHackingPortal.Api.Options;
using Microsoft.Extensions.Options;

namespace GeeksHackingPortal.Api.Services;

/// <summary>
/// Stores activity header images in Cloudflare R2 (S3-compatible API).
/// </summary>
public sealed class HeaderImageStorage : IDisposable
{
    private readonly R2Options _options;
    private readonly AmazonS3Client? _client;

    public HeaderImageStorage(IOptions<R2Options> options)
    {
        _options = options.Value;
        IsConfigured =
            !string.IsNullOrWhiteSpace(_options.AccountId)
            && !string.IsNullOrWhiteSpace(_options.AccessKeyId)
            && !string.IsNullOrWhiteSpace(_options.SecretAccessKey)
            && !string.IsNullOrWhiteSpace(_options.BucketName)
            && !string.IsNullOrWhiteSpace(_options.PublicBaseUrl);

        if (!IsConfigured)
        {
            return;
        }

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            }
        );
    }

    public bool IsConfigured { get; }

    public async Task<string> UploadAsync(
        Guid activityId,
        Stream content,
        string contentType,
        string extension,
        CancellationToken ct
    )
    {
        var key = $"activities/{activityId}/header-{Guid.NewGuid():N}{extension}";
        await Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                DisablePayloadSigning = true,
            },
            ct
        );
        return key;
    }

    public Task DeleteAsync(string key, CancellationToken ct) =>
        Client.DeleteObjectAsync(_options.BucketName, key, ct);

    public string? ToUrl(string? key) => ToUrl(key, _options.PublicBaseUrl);

    public static string? ToUrl(string? key, string publicBaseUrl) =>
        string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(publicBaseUrl) ? null : $"{publicBaseUrl.TrimEnd('/')}/{key}";

    private AmazonS3Client Client =>
        _client ?? throw new InvalidOperationException("R2 header image storage is not configured.");

    public void Dispose() => _client?.Dispose();
}
