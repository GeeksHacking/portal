namespace GeeksHackingPortal.Api.Extensions;

public static class HttpContextRouteExtensions
{
    extension(HttpContext httpContext)
    {
        public Guid? GetHackathonIdFromRoute()
        {
            return httpContext.GetGuidFromRoute("HackathonId");
        }

        public Guid? GetTeamIdFromRoute()
        {
            return httpContext.GetGuidFromRoute("TeamId");
        }

        private Guid? GetGuidFromRoute(string name)
        {
            if (!httpContext.Request.RouteValues.TryGetValue(name, out var raw) || raw is null)
            {
                return null;
            }

            if (!Guid.TryParse(raw.ToString(), out var id))
            {
                return null;
            }

            return id;
        }
    }
}
