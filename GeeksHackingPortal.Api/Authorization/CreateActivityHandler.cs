using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Options;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GeeksHackingPortal.Api.Authorization;

public class CreateActivityHandler(MembershipService membership, IOptions<AppOptions> options)
    : AuthorizationHandler<CreateActivityRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CreateActivityRequirement requirement
    )
    {
        if (context.Resource is not HttpContext)
        {
            return;
        }

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        if (options.Value.CreationMode == ActivityCreationMode.Anyone)
        {
            context.Succeed(requirement);
            return;
        }

        if (await membership.IsRoot(userId.Value))
        {
            context.Succeed(requirement);
        }
    }
}
