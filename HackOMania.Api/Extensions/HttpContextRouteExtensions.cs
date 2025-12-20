namespace HackOMania.Api.Extensions;

public static class HttpContextRouteExtensions
{
    extension(HttpContext httpContext)
    {
        public string? GetHackathonRoute()
        {
            return httpContext.Request.RouteValues.TryGetValue("HackathonId", out var alt)
                ? alt?.ToString()
                : null;
        }

        public string? GetTeamRoute()
        {
            return httpContext.Request.RouteValues.TryGetValue("TeamId", out var value)
                ? value?.ToString()
                : null;
        }
    }
}
