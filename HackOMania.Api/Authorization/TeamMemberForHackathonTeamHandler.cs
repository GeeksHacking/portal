using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;
using static HackOMania.Api.Authorization.AuthorizationHelpers;

namespace HackOMania.Api.Authorization;

public class TeamMemberForHackathonTeamHandler(MembershipService membership)
    : AuthorizationHandler<TeamMemberForHackathonTeamRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamMemberForHackathonTeamRequirement requirement
    )
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return;
        }

        var userId = GetUserId(context.User);
        if (userId is null)
        {
            return;
        }

        var hackathonIdValue = GetHackathonRoute(httpContext);
        var teamIdValue = GetTeamRoute(httpContext);

        if (string.IsNullOrWhiteSpace(hackathonIdValue) || string.IsNullOrWhiteSpace(teamIdValue))
        {
            return;
        }

        var hackathon = await membership.FindHackathon(hackathonIdValue);
        if (hackathon is null)
        {
            return;
        }

        if (!Guid.TryParse(teamIdValue, out var teamId))
        {
            return;
        }

        var participant = await membership.GetParticipant(userId.Value, hackathon.Id);
        if (
            participant?.TeamId == teamId
            || await membership.IsRoot(userId.Value)
            || await membership.IsOrganizer(userId.Value, hackathon.Id)
        )
        {
            context.Succeed(requirement);
        }
    }
}
