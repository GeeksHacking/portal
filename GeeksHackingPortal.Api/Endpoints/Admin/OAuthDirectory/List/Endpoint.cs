using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;
using OpenIddict.Abstractions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.List;

public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext,
    ISqlSugarClient sql
) : EndpointWithoutRequest<Response>
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
        var applications = new List<object>();

        await foreach (var application in applicationManager.ListAsync(cancellationToken: ct))
        {
            applications.Add(application);
        }

        var items = await OAuthDirectoryQueries.ToResponsesAsync(
            applicationManager,
            openIddictDbContext,
            sql,
            applications,
            ct
        );

        await Send.OkAsync(
            new Response
            {
                Items = items
                    .OrderByDescending(i => i.LastAuthorizedAt ?? DateTime.MinValue)
                    .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            },
            ct
        );
    }
}
