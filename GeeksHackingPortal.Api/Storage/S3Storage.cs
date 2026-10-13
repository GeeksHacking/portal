using System.Data.Common;
using Amazon.Runtime;
using Amazon.S3;

namespace GeeksHackingPortal.Api.Storage;

public static class S3Storage
{
    /// <summary>
    /// Reads <see cref="S3StorageSettings"/> from the "Storage" section, then overlays the
    /// "storage" connection string (<c>Endpoint=...;AccessKey=...;SecretKey=...;Bucket=...;Region=...</c>).
    /// </summary>
    public static S3StorageSettings Read(IConfiguration configuration)
    {
        var settings =
            configuration.GetSection(S3StorageSettings.SectionName).Get<S3StorageSettings>()
            ?? new S3StorageSettings();
        var connectionString = configuration.GetConnectionString(S3StorageSettings.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return settings;
        }

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };
        settings.Endpoint = Value(parsed, "Endpoint") ?? settings.Endpoint;
        settings.AccessKey = Value(parsed, "AccessKey") ?? settings.AccessKey;
        settings.SecretKey = Value(parsed, "SecretKey") ?? settings.SecretKey;
        settings.Bucket = Value(parsed, "Bucket") ?? settings.Bucket;
        settings.Region = Value(parsed, "Region") ?? settings.Region;
        return settings;
    }

    public static IAmazonS3 CreateClient(S3StorageSettings settings)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                $"Object storage is not configured. Set the '{S3StorageSettings.ConnectionStringName}' connection string to 'Endpoint=...;AccessKey=...;SecretKey=...;Bucket=...'."
            );
        }

        var config = new AmazonS3Config
        {
            ServiceURL = settings.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = string.IsNullOrWhiteSpace(settings.Region)
                ? "us-east-1"
                : settings.Region,
            UseHttp = settings.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
            // R2 and RustFS reject the flexible checksums the SDK sends by default.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(
            new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
            config
        );
    }

    public static async Task EnsureBucketAsync(
        IAmazonS3 s3,
        S3StorageSettings settings,
        CancellationToken cancellationToken
    )
    {
        // The storage container can report running before it accepts requests.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await s3.PutBucketAsync(settings.Bucket, cancellationToken);
                return;
            }
            catch (AmazonS3Exception exception)
                when (exception.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                return;
            }
            catch (Exception) when (attempt < 10)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private static string? Value(DbConnectionStringBuilder parsed, string key) =>
        parsed.TryGetValue(key, out var value) ? value?.ToString() : null;
}
