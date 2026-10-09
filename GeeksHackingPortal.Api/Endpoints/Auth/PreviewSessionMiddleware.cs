namespace GeeksHackingPortal.Api.Endpoints.Auth;

public sealed class PreviewSessionMiddleware(RequestDelegate next, PreviewSessionTickets tickets)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var header = context.Request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var principal = tickets.ReadSession(header[prefix.Length..].Trim(), context.Request.Headers.Origin.ToString());
                if (principal is not null)
                    context.User = principal;
            }
        }

        await next(context);
    }
}
