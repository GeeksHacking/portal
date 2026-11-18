using System.Text.Json;

namespace GeeksHackingPortal.Api.Options;

/// <summary>
/// S3-compatible bucket used for activity header images: Cloudflare R2 in production, RustFS locally.
/// The bucket is publicly readable via <see cref="PublicBaseUrl"/>; only the API holds write credentials,
/// and browsers upload with short-lived presigned URLs.
/// </summary>
public class R2Options
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Cloudflare account id. Used to build the R2 endpoint when <see cref="ServiceUrl"/> is not set.</summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// S3 endpoint override, e.g. the local RustFS container (http://localhost:9000).
    /// Takes precedence over <see cref="AccountId"/>.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// The only source of the bucket's write credentials: a JSON object
    /// {"accessKeyId": "...", "secretAccessKey": "..."} (Secret Manager secret r2-write-secrets in production).
    /// </summary>
    public string WriteSecrets { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL of the bucket (custom domain or r2.dev), e.g. https://images.example.com
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>Reads the credentials from <see cref="WriteSecrets"/>; empty strings if it is missing or invalid.</summary>
    public (string AccessKeyId, string SecretAccessKey) ResolveCredentials()
    {
        if (string.IsNullOrWhiteSpace(WriteSecrets))
        {
            return (string.Empty, string.Empty);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<R2WriteSecretsPayload>(WriteSecrets, JsonOptions);
            return (parsed?.AccessKeyId ?? string.Empty, parsed?.SecretAccessKey ?? string.Empty);
        }
        catch (JsonException)
        {
            // Invalid JSON: storage treats credentials as unconfigured.
            return (string.Empty, string.Empty);
        }
    }

    private sealed record R2WriteSecretsPayload(string? AccessKeyId, string? SecretAccessKey);

    /// <summary>
    /// Local development only: create the bucket with a public-read policy on startup.
    /// </summary>
    public bool EnsureBucket { get; set; }
}
