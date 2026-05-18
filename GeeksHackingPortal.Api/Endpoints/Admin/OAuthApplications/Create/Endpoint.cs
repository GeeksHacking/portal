using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Create;

public class Endpoint(IOpenIddictApplicationManager applicationManager)
    : Endpoint<Request, OAuthApplicationResponse>
{
    public override void Configure()
    {
        Post("admin/oauth-applications");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Create an OAuth application";
            s.Description =
                "Creates an admin-owned OpenIddict OAuth client for a web or native platform application.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var ownerUserId =
            User.GetUserId() ?? throw new InvalidOperationException("The current user id is required.");

        if (await applicationManager.FindByClientIdAsync(req.ClientId.Trim(), ct) is not null)
        {
            AddError(r => r.ClientId, "An OAuth application with this client id already exists.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var clientSecret =
            req.Platform is OAuthApplicationPlatform.Web
                ? OAuthApplicationMapper.GenerateClientSecret()
                : null;
        var descriptor = OAuthApplicationMapper.ToDescriptor(req, ownerUserId, clientSecret);
        var application = await applicationManager.CreateAsync(descriptor, ct);

        await Send.CreatedAtAsync<Get.Endpoint>(
            new { Id = await applicationManager.GetIdAsync(application, ct) },
            await OAuthApplicationMapper.ToResponseAsync(
                applicationManager,
                application,
                clientSecret,
                ct
            ),
            cancellation: ct
        );
    }
}
