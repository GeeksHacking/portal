using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;

public class Endpoint(IOpenIddictApplicationManager applicationManager) : Endpoint<Request>
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

        for (var attempt = 0; attempt < 2; attempt++)
        {
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

            try
            {
                await applicationManager.DeleteAsync(application, ct);
                await Send.NoContentAsync(ct);
                return;
            }
            catch (OpenIddictExceptions.ConcurrencyException) when (attempt is 0)
            {
            }
        }
    }
}
