using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace GeeksHackingPortal.Api.Authorization;

public class RootHandler(MembershipService membership) : AuthorizationHandler<RootRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RootRequirement requirement
    )
    {
        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        if (await membership.IsRoot(userId.Value))
        {
            context.Succeed(requirement);
        }
    }
}
