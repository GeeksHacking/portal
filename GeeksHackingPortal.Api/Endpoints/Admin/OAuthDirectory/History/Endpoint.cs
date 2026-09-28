using System.Text.Json;
using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.History;

public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext,
    ISqlSugarClient sql
) : Endpoint<Request, Response>
{
    private const int MaxItems = 500;

    public override void Configure()
    {
        Get("admin/oauth-directory/applications/{Id}/history");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Get any OAuth application's authentication history";
            s.Description =
                "Gets the most recent authorizations (sign ins) for any OpenIddict OAuth client, regardless of owner, including the tokens issued for each.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (await applicationManager.FindByIdAsync(req.Id, ct) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var applicationAuthorizations = openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(a => a.Application != null && a.Application.Id == req.Id);

        var totalCount = await applicationAuthorizations.CountAsync(ct);

        var authorizations = await applicationAuthorizations
            .OrderByDescending(a => a.CreationDate)
            .Take(MaxItems)
            .Select(a => new
            {
                a.Id,
                a.Subject,
                a.CreationDate,
                a.Status,
                a.Type,
                a.Scopes,
            })
            .ToListAsync(ct);

        var authorizationIds = authorizations.Select(a => a.Id).ToList();

        var tokenStats = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(t => t.Authorization != null && authorizationIds.Contains(t.Authorization.Id))
            .GroupBy(t => t.Authorization!.Id)
            .Select(g => new
            {
                AuthorizationId = g.Key!,
                TokenCount = g.Count(),
                LastTokenIssuedAt = g.Max(t => t.CreationDate),
            })
            .ToDictionaryAsync(s => s.AuthorizationId, ct);

        var userIds = authorizations
            .Select(a => Guid.TryParse(a.Subject, out var userId) ? userId : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, User>()
            : (await sql.Queryable<User>().Where(u => userIds.Contains(u.Id)).ToListAsync(ct))
                .ToDictionary(u => u.Id);

        var items = authorizations
            .Select(a =>
            {
                var userId = Guid.TryParse(a.Subject, out var parsedUserId) ? parsedUserId : (Guid?)null;
                User? user = null;
                if (userId.HasValue)
                {
                    users.TryGetValue(userId.Value, out user);
                }

                tokenStats.TryGetValue(a.Id!, out var tokenStat);

                return new OAuthDirectoryHistoryItemResponse
                {
                    Id = a.Id!,
                    Subject = a.Subject ?? string.Empty,
                    UserId = userId,
                    UserName = user?.Name,
                    UserEmail = user?.Email,
                    CreationDate = a.CreationDate.AsUtcDateTimeOffset(),
                    Status = a.Status,
                    Type = a.Type,
                    Scopes = ParseScopes(a.Scopes),
                    TokenCount = tokenStat?.TokenCount ?? 0,
                    LastTokenIssuedAt = tokenStat?.LastTokenIssuedAt.AsUtcDateTimeOffset(),
                };
            })
            .ToList();

        await Send.OkAsync(new Response { TotalCount = totalCount, Items = items }, ct);
    }

    private static IReadOnlyList<string> ParseScopes(string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(scopes) ?? [];
        }
        catch (JsonException)
        {
            return [scopes];
        }
    }
}
