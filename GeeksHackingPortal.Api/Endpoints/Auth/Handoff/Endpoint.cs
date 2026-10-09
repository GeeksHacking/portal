using FastEndpoints;

namespace GeeksHackingPortal.Api.Endpoints.Auth.Handoff;

public class Request
{
    public string Code { get; set; } = "";
}

public class Response
{
    public string Token { get; set; } = "";
}

public class Endpoint(PreviewSessionTickets tickets) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("auth/handoff");
        AllowAnonymous();
        Description(d => d.ExcludeFromDescription());
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var origin = HttpContext.Request.Headers.Origin.ToString();
        var token = tickets.Redeem(req.Code, origin);
        if (token is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new Response { Token = token }, ct);
    }
}
