using HackOMania.Api.Extensions;
using HackOMania.Api.Options;
using HackOMania.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HackOMania.Api.Authorization;

public class CreateHackathonHandler(MembershipService membership, IOptions<AppOptions> options)
    : AuthorizationHandler<CreateHackathonRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CreateHackathonRequirement requirement
    )
    {
        var userId = context.User.GetUserId<Guid>();

        if (options.Value.CreationMode == HackathonCreationMode.Anyone)
        {
            context.Succeed(requirement);
            return;
        }

        if (await membership.IsRoot(userId))
        {
            context.Succeed(requirement);
        }
    }
}
