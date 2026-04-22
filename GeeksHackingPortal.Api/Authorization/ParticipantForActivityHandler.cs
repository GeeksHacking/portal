using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace GeeksHackingPortal.Api.Authorization;

public class ParticipantForActivityHandler(MembershipService membership)
    : AuthorizationHandler<ParticipantForActivityRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ParticipantForActivityRequirement requirement
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

        if (
            await membership.IsRoot(userId.Value)
            || await membership.IsActivityOrganizer(userId.Value, activityId.Value)
            || await membership.IsActivityRegistered(userId.Value, activityId.Value)
        )
        {
            context.Succeed(requirement);
        }
    }
}
