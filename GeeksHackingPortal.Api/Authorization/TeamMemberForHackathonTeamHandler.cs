using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;

namespace GeeksHackingPortal.Api.Authorization;

public class TeamMemberForHackathonTeamHandler(MembershipService membership, ISqlSugarClient sql)
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

        var userId = context.User.GetUserId();
        if (userId is null)
        {
            return;
        }

        var hackathonIdValue = httpContext.GetHackathonIdFromRoute();
        var teamIdValue = httpContext.GetTeamIdFromRoute();

        if (hackathonIdValue is null || teamIdValue is null)
        {
            return;
        }

        var hackathon = await sql.Queryable<Hackathon>().InSingleAsync(hackathonIdValue.Value);
        if (hackathon is null)
        {
            return;
        }

        var participant = await membership.GetParticipant(userId.Value, hackathon.Id);
        if (
            participant?.TeamId == teamIdValue
            || await membership.IsRoot(userId.Value)
            || await membership.IsOrganizer(userId.Value, hackathon.Id)
        )
        {
            context.Succeed(requirement);
        }
    }
}
