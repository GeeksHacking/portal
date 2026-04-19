using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Services;

namespace GeeksHackingPortal.Api.Middleware;

public class BannedUserMiddleware(RequestDelegate next)
{
    private static readonly PathString[] AllowedPaths =
    [
        new("/auth/login"),
        new("/auth/impersonate"),
        new("/callback/login/github"),
    ];

    public async Task InvokeAsync(HttpContext context, MembershipService membership)
    {
        if (
            AllowedPaths.Any(path => context.Request.Path.StartsWithSegments(path))
            || context.User.Identity?.IsAuthenticated != true
        )
        {
            await next(context);
            return;
        }

        var userIdRaw = context.User.Claims
            .FirstOrDefault(claim => claim.Type == CustomClaimTypes.UserId)
            ?.Value;

        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var user = await membership.GetUser(userId, context.RequestAborted);
        if (user is null || membership.IsBanned(user))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }
}
