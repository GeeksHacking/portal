using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Services;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Get;

public class Endpoint(IOpenIddictApplicationManager applicationManager, OAuthDirectoryService oauthDirectory)
    : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("admin/oauth-directory/applications/{Id}");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Get any OAuth application";
            s.Description =
                "Gets the details of any OpenIddict OAuth client, regardless of owner, including its permissions and usage statistics.";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var application = await applicationManager.FindByIdAsync(req.Id, ct);

        if (application is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                Application = await oauthDirectory.GetApplicationAsync(application, ct),
                Permissions = (await applicationManager.GetPermissionsAsync(application, ct))
                    .Order(StringComparer.Ordinal)
                    .ToList(),
                Requirements = (await applicationManager.GetRequirementsAsync(application, ct))
                    .Order(StringComparer.Ordinal)
                    .ToList(),
            },
            ct
        );
    }
}
