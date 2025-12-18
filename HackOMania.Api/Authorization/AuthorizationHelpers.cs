using System.Security.Claims;

namespace HackOMania.Api.Authorization;

internal static class AuthorizationHelpers
{
    internal static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim is null)
        {
            return null;
        }

        return Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    internal static string? GetHackathonRoute(HttpContext httpContext)
    {
        if (httpContext.Request.RouteValues.TryGetValue("Id", out var value))
        {
            return value?.ToString();
        }

        if (httpContext.Request.RouteValues.TryGetValue("HackathonId", out var alt))
        {
            return alt?.ToString();
        }

        return null;
    }

    internal static string? GetTeamRoute(HttpContext httpContext)
    {
        return httpContext.Request.RouteValues.TryGetValue("TeamId", out var value)
            ? value?.ToString()
            : null;
    }
}
