using FastEndpoints;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Extensions;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;

public class Endpoint(OAuthApplicationDeletionService deletionService) : Endpoint<Request>
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

        var result = await deletionService.DeleteOwnedAsync(req.Id, ownerUserId, ct);

        if (result is OAuthApplicationDeletionResult.NotFound)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
