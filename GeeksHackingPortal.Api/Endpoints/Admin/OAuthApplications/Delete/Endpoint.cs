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
    OpenIddictDbContext db
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

        var applicationId = await applicationManager.GetIdAsync(application, ct);
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var applicationStillExists = await db
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .AnyAsync(app => app.Id == applicationId, ct);

        if (!applicationStillExists)
        {
            await transaction.RollbackAsync(ct);
            await Send.NotFoundAsync(ct);
            return;
        }

        await db
            .Set<OpenIddictEntityFrameworkCoreToken>()
            .Where(token =>
                token.ApplicationId == applicationId
                || db
                    .Set<OpenIddictEntityFrameworkCoreAuthorization>()
                    .Any(authorization =>
                        authorization.Id == token.AuthorizationId
                        && authorization.ApplicationId == applicationId
                    )
            )
            .ExecuteDeleteAsync(ct);

        await db
            .Set<OpenIddictEntityFrameworkCoreAuthorization>()
            .Where(authorization => authorization.ApplicationId == applicationId)
            .ExecuteDeleteAsync(ct);

        var deletedApplications = await db
            .Set<OpenIddictEntityFrameworkCoreApplication>()
            .Where(app => app.Id == applicationId)
            .ExecuteDeleteAsync(ct);

        if (deletedApplications is 0)
        {
            await transaction.RollbackAsync(ct);
            await Send.NotFoundAsync(ct);
            return;
        }

        await transaction.CommitAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
