using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;
using GeeksHackingPortal.Api.Extensions;
using OpenIddict.Abstractions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.List;

public class Endpoint(IOpenIddictApplicationManager applicationManager)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("admin/oauth-applications");
        Policies(PolicyNames.Root);
        Description(b => b.WithTags("Admin"));
        Summary(s =>
        {
            s.Summary = "List OAuth applications";
            s.Description = "Lists OpenIddict OAuth clients owned by the current admin.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var ownerUserId =
            User.GetUserId() ?? throw new InvalidOperationException("The current user id is required.");
        var items = new List<OAuthApplicationResponse>();

        await foreach (var application in applicationManager.ListAsync(cancellationToken: ct))
        {
            if (
                await OAuthApplicationMapper.IsOwnedByAsync(
                    applicationManager,
                    application,
                    ownerUserId,
                    ct
                )
            )
            {
                items.Add(
                    await OAuthApplicationMapper.ToResponseAsync(
                        applicationManager,
                        application,
                        clientSecret: null,
                        ct
                    )
                );
            }
        }

        await Send.OkAsync(new Response { Items = items }, ct);
    }
}
