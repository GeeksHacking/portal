using HackOMania.Api.Entities;
using HackOMania.Api.Extensions;
using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;
using SqlSugar;

namespace HackOMania.Api.Authorization;

public class OrganizerForHackathonHandler(MembershipService membership, ISqlSugarClient sql)
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

        var idValue = httpContext.GetHackathonIdFromRoute();
        if (idValue is null)
        {
            return;
        }

        var hackathon = await sql.Queryable<Hackathon>().InSingleAsync(idValue.Value);
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
