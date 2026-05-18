using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;

public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext
) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("admin/oauth-applications/{Id}");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Delete an OAuth application";
            s.Description = "Deletes an OpenIddict OAuth client owned by the current admin.";
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

        await using var transaction = await openIddictDbContext.Database.BeginTransactionAsync(ct);

        var authorizationIds = await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => EF.Property<string?>(authorization, "ApplicationId") == req.Id)
            .Select(authorization => authorization.Id)
            .ToListAsync(ct);

        await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(token => EF.Property<string?>(token, "ApplicationId") == req.Id)
            .ExecuteDeleteAsync(ct);

        if (authorizationIds.Count > 0)
        {
            await openIddictDbContext
                .Set<OpenIddictEntityFrameworkCoreToken>()
                .Where(token =>
                    authorizationIds.Contains(EF.Property<string>(token, "AuthorizationId"))
                )
                .ExecuteDeleteAsync(ct);
        }

        await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => EF.Property<string?>(authorization, "ApplicationId") == req.Id)
            .ExecuteDeleteAsync(ct);

        await openIddictDbContext
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .Where(currentApplication => currentApplication.Id == req.Id)
            .ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
