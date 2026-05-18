using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;

public enum OAuthApplicationDeletionResult
{
    Deleted,
    NotFound,
}

public interface IOAuthApplicationDeletionOperations
{
    Task<object?> FindByIdAsync(string applicationId, CancellationToken ct);
    ValueTask<bool> IsOwnedByAsync(object application, Guid ownerUserId, CancellationToken ct);
    Task DeleteAsync(object application, CancellationToken ct);
}

public sealed class OpenIddictOAuthApplicationDeletionOperations(
    IOpenIddictApplicationManager applicationManager
) : IOAuthApplicationDeletionOperations
{
    public Task<object?> FindByIdAsync(string applicationId, CancellationToken ct) =>
        applicationManager.FindByIdAsync(applicationId, ct);

    public ValueTask<bool> IsOwnedByAsync(object application, Guid ownerUserId, CancellationToken ct) =>
        OAuthApplicationMapper.IsOwnedByAsync(applicationManager, application, ownerUserId, ct);

    public Task DeleteAsync(object application, CancellationToken ct) =>
        applicationManager.DeleteAsync(application, ct);
}

public class OAuthApplicationDeletionService(
    IOAuthApplicationDeletionOperations operations,
    ILogger<OAuthApplicationDeletionService> logger
)
{
    private const int MaxDeleteAttempts = 3;

    public async Task<OAuthApplicationDeletionResult> DeleteOwnedAsync(
        string applicationId,
        Guid ownerUserId,
        CancellationToken ct
    )
    {
        for (var attempt = 0; attempt < MaxDeleteAttempts; attempt++)
        {
            var application = await operations.FindByIdAsync(applicationId, ct);
            if (application is null || !await operations.IsOwnedByAsync(application, ownerUserId, ct))
            {
                return OAuthApplicationDeletionResult.NotFound;
            }

            try
            {
                await operations.DeleteAsync(application, ct);
                return OAuthApplicationDeletionResult.Deleted;
            }
            catch (OpenIddictExceptions.ConcurrencyException) when (attempt < MaxDeleteAttempts - 1)
            {
                logger.LogWarning(
                    "OAuth application delete hit a concurrency conflict for application id {ApplicationId}; reloading the latest application state and retrying.",
                    applicationId
                );
            }
        }

        throw new InvalidOperationException(
            "OAuth application deletion exceeded the configured number of retry attempts."
        );
    }
}
