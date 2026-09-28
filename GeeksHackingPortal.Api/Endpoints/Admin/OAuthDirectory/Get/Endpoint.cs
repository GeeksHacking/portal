using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;
using OpenIddict.Abstractions;
using SqlSugar;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Get;

public class Endpoint(
    IOpenIddictApplicationManager applicationManager,
    OpenIddictDbContext openIddictDbContext,
    ISqlSugarClient sql
) : Endpoint<Request, Response>
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

        var summary = (
            await OAuthDirectoryQueries.ToResponsesAsync(
                applicationManager,
                openIddictDbContext,
                sql,
                [application],
                ct
            )
        ).Single();

        await Send.OkAsync(
            new Response
            {
                Application = summary,
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
