namespace GeeksHackingPortal.Api.Endpoints.Auth;

public interface IPreviewSessionHandoffStore
{
    Task RegisterAsync(
        string nonceHash,
        string origin,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    );

    Task<bool> TryConsumeAsync(
        string nonceHash,
        string origin,
        DateTimeOffset now,
        CancellationToken cancellationToken
    );
}
