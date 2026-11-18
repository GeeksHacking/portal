namespace GeeksHackingPortal.Api.Options;

/// <summary>
/// Cloudflare R2 bucket used for activity header images.
/// The bucket is publicly readable via <see cref="PublicBaseUrl"/>; only the API holds write credentials.
/// </summary>
public class R2Options
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL of the bucket (custom domain or r2.dev), e.g. https://images.example.com
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
