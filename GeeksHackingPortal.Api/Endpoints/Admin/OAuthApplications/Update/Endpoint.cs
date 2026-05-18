using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Update;

public class Endpoint(IOpenIddictApplicationManager applicationManager)
    : Endpoint<Request, OAuthApplicationResponse>
{
    public override void Configure()
    {
        Put("admin/oauth-applications/{Id}");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Update an OAuth application";
            s.Description =
                "Updates an admin-owned OpenIddict OAuth client. Set rotateClientSecret to true to issue a new web client secret.";
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

        var existingClientId = await applicationManager.GetClientIdAsync(application, ct);
        var requestedClientId = req.ClientId.Trim();
        if (!string.Equals(existingClientId, requestedClientId, StringComparison.Ordinal))
        {
            var conflictingApplication = await applicationManager.FindByClientIdAsync(
                requestedClientId,
                ct
            );
            if (conflictingApplication is not null)
            {
                AddError(r => r.ClientId, "An OAuth application with this client id already exists.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }
        }

        var currentDescriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(currentDescriptor, application, ct);
        var currentApplicationType = await applicationManager.GetApplicationTypeAsync(application, ct);

        var shouldIssueClientSecret =
            req.Platform is OAuthApplicationPlatform.Web
            && (
                req.RotateClientSecret
                || currentApplicationType is not ApplicationTypes.Web
                || string.IsNullOrWhiteSpace(currentDescriptor.ClientSecret)
            );
        var clientSecret = shouldIssueClientSecret
            ? OAuthApplicationMapper.GenerateClientSecret()
            : currentDescriptor.ClientSecret;
        var descriptor = OAuthApplicationMapper.ToDescriptor(req, ownerUserId, clientSecret);

        if (shouldIssueClientSecret)
        {
            await applicationManager.UpdateAsync(application, descriptor, ct);
        }
        else
        {
            await applicationManager.PopulateAsync(application, descriptor, ct);
            await applicationManager.UpdateAsync(application, ct);
        }

        await Send.OkAsync(
            await OAuthApplicationMapper.ToResponseAsync(
                applicationManager,
                application,
                shouldIssueClientSecret ? clientSecret : null,
                ct
            ),
            ct
        );
    }
}
