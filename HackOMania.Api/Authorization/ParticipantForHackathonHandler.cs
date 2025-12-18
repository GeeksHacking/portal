using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace HackOMania.Api.Authorization;

public class ParticipantForHackathonHandler(MembershipService membership)
    : AuthorizationHandler<ParticipantForHackathonRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ParticipantForHackathonRequirement requirement
    )
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }

        var userId = AuthorizationHelpers.GetUserId(context.User);
        if (userId is null)
        {
            return;
        }

        var idValue = AuthorizationHelpers.GetHackathonRoute(httpContext);
        if (string.IsNullOrWhiteSpace(idValue))
        {
            return;
        }

        var hackathon = await membership.FindHackathon(idValue);
        if (hackathon is null)
        {
            return;
        }

        if (
            await membership.IsRoot(userId.Value)
            || await membership.IsOrganizer(userId.Value, hackathon.Id)
            || await membership.IsParticipant(userId.Value, hackathon.Id)
        )
        {
            context.Succeed(requirement);
        }
    }
}
