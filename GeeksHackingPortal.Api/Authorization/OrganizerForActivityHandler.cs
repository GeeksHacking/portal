using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace GeeksHackingPortal.Api.Authorization;

public class OrganizerForActivityHandler(MembershipService membership)
    : AuthorizationHandler<OrganizerForActivityRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizerForActivityRequirement requirement
    )
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        var activityId = httpContext.GetActivityIdFromRoute();
        if (activityId is null)
        {
            return;
        }

        if (await membership.IsActivityOrganizerOrRoot(userId.Value, activityId.Value))
        {
            context.Succeed(requirement);
        }
    }
}
