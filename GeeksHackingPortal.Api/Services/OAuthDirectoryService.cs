using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;
using GeeksHackingPortal.Api.Entities;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using SqlSugar;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GeeksHackingPortal.Api.Services;

/// <summary>
/// Root-wide view of OAuth applications regardless of which admin owns them.
/// </summary>
public class OAuthDirectoryService(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext,
    ISqlSugarClient sql
)
{
    public async Task<List<OAuthDirectoryApplicationResponse>> ListApplicationsAsync(CancellationToken ct)
    {
        var summaries = new List<ApplicationSummary>();

        await foreach (var application in applicationManager.ListAsync(cancellationToken: ct))
        {
            summaries.Add(await SummarizeAsync(application, ct));
        }

        return await ToResponsesAsync(summaries, ct);
    }

    public async Task<OAuthDirectoryApplicationResponse> GetApplicationAsync(object application, CancellationToken ct)
    {
        var responses = await ToResponsesAsync([await SummarizeAsync(application, ct)], ct);
        return responses.Single();
    }

    private async Task<ApplicationSummary> SummarizeAsync(object application, CancellationToken ct) =>
        new(
            await OAuthApplicationMapper.ToResponseAsync(applicationManager, application, clientSecret: null, ct),
            await OAuthApplicationMapper.GetOwnerUserIdAsync(applicationManager, application, ct),
            await applicationManager.GetClientTypeAsync(application, ct),
            await applicationManager.GetConsentTypeAsync(application, ct)
        );

    private async Task<List<OAuthDirectoryApplicationResponse>> ToResponsesAsync(
        List<ApplicationSummary> summaries,
        CancellationToken ct
    )
    {
        var applicationIds = summaries.Select(s => s.Application.Id).ToList();

        var authorizationStats = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(a => a.Application != null && applicationIds.Contains(a.Application.Id))
            .GroupBy(a => a.Application!.Id)
            .Select(g => new
            {
                ApplicationId = g.Key!,
                TotalAuthorizations = g.Count(),
                ValidAuthorizations = g.Count(a => a.Status == Statuses.Valid),
                UniqueUsers = g.Select(a => a.Subject).Distinct().Count(),
                LastAuthorizedAt = g.Max(a => a.CreationDate),
            })
            .ToDictionaryAsync(s => s.ApplicationId, ct);

        var tokenStats = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(t => t.Application != null && applicationIds.Contains(t.Application.Id))
            .GroupBy(t => t.Application!.Id)
            .Select(g => new
            {
                ApplicationId = g.Key!,
                TotalTokens = g.Count(),
                LastTokenIssuedAt = g.Max(t => t.CreationDate),
            })
            .ToDictionaryAsync(s => s.ApplicationId, ct);

        var ownerIds = summaries
            .Where(s => s.OwnerUserId.HasValue)
            .Select(s => s.OwnerUserId!.Value)
            .Distinct()
            .ToList();
        var owners = ownerIds.Count == 0
            ? new Dictionary<Guid, User>()
            : (await sql.Queryable<User>().Where(u => ownerIds.Contains(u.Id)).ToListAsync(ct))
                .ToDictionary(u => u.Id);

        return summaries
            .Select(s =>
            {
                authorizationStats.TryGetValue(s.Application.Id, out var authorizationStat);
                tokenStats.TryGetValue(s.Application.Id, out var tokenStat);
                User? owner = null;
                if (s.OwnerUserId.HasValue)
                {
                    owners.TryGetValue(s.OwnerUserId.Value, out owner);
                }

                return new OAuthDirectoryApplicationResponse
                {
                    Id = s.Application.Id,
                    ClientId = s.Application.ClientId,
                    DisplayName = s.Application.DisplayName,
                    Platform = s.Application.Platform,
                    ClientType = s.ClientType,
                    ConsentType = s.ConsentType,
                    RedirectUris = s.Application.RedirectUris,
                    PostLogoutRedirectUris = s.Application.PostLogoutRedirectUris,
                    OwnerUserId = s.OwnerUserId,
                    OwnerName = owner?.Name,
                    OwnerEmail = owner?.Email,
                    TotalAuthorizations = authorizationStat?.TotalAuthorizations ?? 0,
                    ValidAuthorizations = authorizationStat?.ValidAuthorizations ?? 0,
                    UniqueUsers = authorizationStat?.UniqueUsers ?? 0,
                    TotalTokens = tokenStat?.TotalTokens ?? 0,
                    LastAuthorizedAt = authorizationStat?.LastAuthorizedAt,
                    LastTokenIssuedAt = tokenStat?.LastTokenIssuedAt,
                };
            })
            .ToList();
    }

    private sealed record ApplicationSummary(
        OAuthApplicationResponse Application,
        Guid? OwnerUserId,
        string? ClientType,
        string? ConsentType
    );
}
