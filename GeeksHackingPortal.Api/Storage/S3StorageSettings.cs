namespace GeeksHackingPortal.Api.Storage;

/// <summary>
/// S3-compatible object storage settings. Cloudflare R2 in production, RustFS in local development.
/// Read from the "storage" connection string, falling back to the "Storage" configuration section.
/// </summary>
public sealed class S3StorageSettings
{
    public const string SectionName = "Storage";

    public const string ConnectionStringName = "storage";

    public string Endpoint { get; set; } = "";

    public string AccessKey { get; set; } = "";

    public string SecretKey { get; set; } = "";

    public string Bucket { get; set; } = "geekshacking-portal";

    /// <summary>
    /// Signing region. R2 expects "auto".
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Create the bucket on startup when it is missing. Only enabled for local development, where the
    /// RustFS container starts empty; production R2 tokens are scoped to objects and cannot create buckets.
    /// </summary>
    public bool CreateBucket { get; set; }

    /// <summary>
    /// How long download URLs handed to clients stay valid.
    /// </summary>
    public TimeSpan DownloadUrlLifetime { get; set; } = TimeSpan.FromHours(1);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(Bucket);
}
