using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.History;


public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext,
    ISqlSugarClient sql
) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("admin/oauth-applications/{Id}/history");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Get OAuth application sign in history";
            s.Description = "Gets recent sign in history for a specific OAuth application owned by the current admin.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var ownerUserId =
            User.GetUserId() ?? throw new InvalidOperationException("The current user id is required.");

        var application = await applicationManager.FindByIdAsync(req.Id, ct);

        if (
            application is null
            || !await OAuthApplicationMapper.IsOwnedByAsync(
                applicationManager,
                application,
                ownerUserId,
                ct
            )
        )
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var authorizations = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(a => a.Application != null && a.Application.Id == req.Id && a.Subject != null)
            .OrderByDescending(a => a.CreationDate)
            .Take(100)
            .ToListAsync(ct);

        var userIdsStr = authorizations.Select(a => a.Subject!).Distinct().ToList();
        var userIds = userIdsStr.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();

        var users = await sql.Queryable<User>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);

        var userMap = users.ToDictionary(u => u.Id.ToString());

        var items = authorizations.Select(a => new OAuthApplicationHistoryItemResponse
        {
            Id = a.Id,
            Subject = a.Subject!,
            UserName = userMap.TryGetValue(a.Subject!, out var u) ? u.Name : "Unknown",
            UserEmail = userMap.TryGetValue(a.Subject!, out var ue) ? ue.Email : "Unknown",
            CreationDate = a.CreationDate,
            Scopes = a.Scopes
        }).ToList();

        await Send.OkAsync(new Response { Items = items }, ct);
    }
}


