using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Services;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.List;

public class Endpoint(OAuthDirectoryService oauthDirectory) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("admin/oauth-directory/applications");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "List all OAuth applications";
            s.Description =
                "Lists every OpenIddict OAuth client across all owners, with owner details and usage statistics.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await oauthDirectory.ListApplicationsAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Items = items
                    .OrderByDescending(i => i.LastAuthorizedAt ?? DateTimeOffset.MinValue)
                    .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            },
            ct
        );
    }
}
