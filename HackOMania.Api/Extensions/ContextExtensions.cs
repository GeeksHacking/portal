namespace HackOMania.Api.Extensions;

public static class ContextExtensions
{
    extension(HttpContext context)
    {
        public string? GetHackathonRoute()
        {
            if (context.Request.RouteValues.TryGetValue("Id", out var value))
            {
                return value?.ToString();
            }

            if (context.Request.RouteValues.TryGetValue("HackathonId", out var alt))
            {
                return alt?.ToString();
            }

            return null;
        }

        public string? GetTeamRoute()
        {
            return context.Request.RouteValues.TryGetValue("TeamId", out var value)
                ? value?.ToString()
                : null;
        }
    }
}
