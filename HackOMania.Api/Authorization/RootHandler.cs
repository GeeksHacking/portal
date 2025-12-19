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
        var userId = context.User.GetUserId<Guid>();
        if (await membership.IsRoot(userId))
        {
            context.Succeed(requirement);
        }
    }
}
