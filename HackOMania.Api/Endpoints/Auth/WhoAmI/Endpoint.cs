using System.Security.Claims;
using FastEndpoints;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using HackOMania.Api.Services;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Auth.WhoAmI;

public class Endpoint(ISqlSugarClient sql, MembershipService membership)
    : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("auth/whoami");
        Description(b => b.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Get current user info";
            s.Description =
                "Returns the current authenticated user's information including GitHub details.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var gh = await sql.Queryable<GitHubOnlineAccount>()
            .Includes(g => g.User)
            .Where(a =>
                a.Id.ToString()
                == User.Claims.First(c => c.Type == CustomClaimTypes.GitHubAccountId).Value
            )
            .FirstAsync(ct);

        await Send.OkAsync(
            new Response
            {
                Id = gh.User.Id,
                Name = gh.User.Name,
                Email = gh.User.Email,
                GitHubId = gh.GitHubId,
                GitHubLogin = gh.GitHubLogin,
                IsRoot = membership.IsAdmin(gh.User),
            },
            ct
        );
    }
}
