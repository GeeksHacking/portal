using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Extensions;
using GeeksHackingPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;

namespace GeeksHackingPortal.Api.Authorization;

public class TeamCreatorForHackathonTeamHandler(MembershipService membership, ISqlSugarClient sql)
    : AuthorizationHandler<TeamCreatorForHackathonTeamRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeamCreatorForHackathonTeamRequirement requirement
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

        // Check if user is the team creator
        var team = await sql.Queryable<Team>()
            .Where(t => t.Id == teamIdValue && t.HackathonId == hackathon.Id)
            .FirstAsync();

        if (team?.CreatedByUserId == userId)
        {
            context.Succeed(requirement);
        }
    }
}
