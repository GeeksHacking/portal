using System.Security.Claims;
using FastEndpoints;
using FastEndpoints.Security;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using HackOMania.Api.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SqlSugar;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace HackOMania.Api.Endpoints.Callback.Login.GitHub;

public class Endpoint(
    IOptions<AppOptions> options,
    IOptions<AdminOptions> adminOptions,
    ISqlSugarClient db
) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Routes("callback/login/github");
        Verbs(Http.POST, Http.GET);
        AllowAnonymous();

        Description(d => d.ExcludeFromDescription());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(Providers.GitHub);
        if (!result.Succeeded || result.Principal is null)
        {
            await Send.ForbiddenAsync(cancellation: ct);
            return;
        }

        var githubLogin = result.Principal.GetClaim("login");
        var githubId = result.Principal.GetClaim(ClaimTypes.NameIdentifier);
        var email = result.Principal.GetClaim(ClaimTypes.Email);
        var name = result.Principal.GetClaim(ClaimTypes.Name) ?? "HackOMan";

        ArgumentException.ThrowIfNullOrWhiteSpace(githubLogin);
        ArgumentException.ThrowIfNullOrWhiteSpace(githubId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var existingAccount = await db.Queryable<GitHubOnlineAccount>()
            .Includes(a => a.User)
            .Where(a => a.GitHubLogin == githubLogin)
            .SingleAsync();

        User? accountUser;
        Guid githubAccountId;

        if (existingAccount is null)
        {
            var existingUser = !string.IsNullOrEmpty(email)
                ? await db.Queryable<User>().Where(u => u.Email == email).SingleAsync()
                : null;

            if (existingUser is null)
            {
                accountUser = new User { Name = name, Email = email };
                var newAccount = new GitHubOnlineAccount
                {
                    GitHubLogin = githubLogin,
                    GitHubId = long.Parse(githubId),
                    User = accountUser,
                };
                await db.InsertNav(newAccount).Include(a => a.User).ExecuteCommandAsync();
                githubAccountId = newAccount.Id;
            }
            else
            {
                accountUser = existingUser;
                var newAccount = new GitHubOnlineAccount
                {
                    GitHubLogin = githubLogin,
                    GitHubId = long.Parse(githubId),
                    UserId = existingUser.Id,
                };
                await db.Insertable(newAccount).ExecuteCommandAsync(ct);
                githubAccountId = newAccount.Id;
            }
        }
        else
        {
            accountUser = existingAccount.User;
            githubAccountId = existingAccount.Id;
        }

        await CookieAuth.SignInAsync(o =>
        {
            o.Claims.AddRange(result.Principal.Claims);
            o.Claims.Add(new Claim(CustomClaimTypes.UserId, accountUser.Id.ToString()));
            o.Claims.Add(new Claim(CustomClaimTypes.GitHubLogin, githubAccountId.ToString()));
        });

        await Send.RedirectAsync(options.Value.FrontendUrl, allowRemoteRedirects: true);
    }
}
