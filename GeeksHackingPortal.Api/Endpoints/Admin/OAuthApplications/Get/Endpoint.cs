using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Get;

public class Endpoint(IOpenIddictApplicationManager applicationManager)
    : Endpoint<Request, OAuthApplicationResponse>
{
    public override void Configure()
    {
        Get("admin/oauth-applications/{Id}");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "Get an OAuth application";
            s.Description = "Gets an OpenIddict OAuth client owned by the current admin.";
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

        await Send.OkAsync(
            await OAuthApplicationMapper.ToResponseAsync(
                applicationManager,
                application,
                clientSecret: null,
                ct
            ),
            ct
        );
    }
}
