using System.Text.Json;

namespace GeeksHackingPortal.Api.Options;

/// <summary>
/// Cloudflare R2 bucket used for activity header images.
/// The bucket is publicly readable via <see cref="PublicBaseUrl"/>; only the API holds write credentials.
/// </summary>
public class R2Options
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// JSON object containing write credentials: {"accessKeyId": "...", "secretAccessKey": "..."}
    /// </summary>
    public string WriteSecrets { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL of the bucket (custom domain or r2.dev), e.g. https://images.example.com
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public (string AccessKeyId, string SecretAccessKey) ResolveCredentials()
    {
        if (!string.IsNullOrWhiteSpace(AccessKeyId) && !string.IsNullOrWhiteSpace(SecretAccessKey))
        {
            return (AccessKeyId, SecretAccessKey);
        }

        if (!string.IsNullOrWhiteSpace(WriteSecrets))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<R2WriteSecretsPayload>(WriteSecrets, JsonOptions);
                if (parsed is not null)
                {
                    return (
                        !string.IsNullOrWhiteSpace(AccessKeyId) ? AccessKeyId : parsed.AccessKeyId ?? string.Empty,
                        !string.IsNullOrWhiteSpace(SecretAccessKey) ? SecretAccessKey : parsed.SecretAccessKey ?? string.Empty
                    );
                }
            }
            catch (JsonException)
            {
                // Ignore invalid JSON; storage will treat credentials as unconfigured.
            }
        }

        return (AccessKeyId, SecretAccessKey);
    }

    private sealed record R2WriteSecretsPayload(string? AccessKeyId, string? SecretAccessKey);
}
