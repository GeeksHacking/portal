using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace HackOMania.Api.Authorization;

public class RootHandler(MembershipService membership) : AuthorizationHandler<RootRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RootRequirement requirement
    )
    {
        var userId = context.User.GetUserId();
        if (!userId.HasValue)
        {
            return;
        }

        if (await membership.IsRoot(userId.Value))
        {
            context.Succeed(requirement);
        }
    }
}
