using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Endpoints.Auth;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services;

public sealed class SqlSugarPreviewSessionHandoffStore(ISqlSugarClient db)
    : IPreviewSessionHandoffStore
{
    public async Task RegisterAsync(
        string nonceHash,
        string origin,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    )
    {
        await db.Deleteable<PreviewSessionHandoff>()
            .Where(handoff => handoff.ExpiresAt <= DateTimeOffset.UtcNow)
            .ExecuteCommandAsync(cancellationToken);

        await db.Insertable(new PreviewSessionHandoff
        {
            NonceHash = nonceHash,
            Origin = origin,
            ExpiresAt = expiresAt,
        }).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<bool> TryConsumeAsync(
        string nonceHash,
        string origin,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var deleted = await db.Deleteable<PreviewSessionHandoff>()
            .Where(handoff =>
                handoff.NonceHash == nonceHash
                && handoff.Origin == origin
                && handoff.ExpiresAt > now
            )
            .ExecuteCommandAsync(cancellationToken);
        return deleted == 1;
    }
}
