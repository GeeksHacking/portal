using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace HackOMania.Api.Authorization;

public class OrganizerForHackathonHandler(MembershipService membership)
    : AuthorizationHandler<OrganizerForHackathonRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizerForHackathonRequirement requirement
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

        var idValue = HttpContextRouteExtensions.GetHackathonRoute(httpContext);
        if (string.IsNullOrWhiteSpace(idValue))
        {
            return;
        }

        var hackathon = await membership.FindHackathon(idValue);
        if (hackathon is null)
        {
            return;
        }

        if (await membership.IsOrganizerOrRoot(userId.Value, hackathon.Id))
        {
            context.Succeed(requirement);
        }
    }
}
