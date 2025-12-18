using System.Security.Claims;
using Dm.util;
using FastEndpoints;
using FastEndpoints.Security;
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
            .Where(a => a.GitHubLogin == githubLogin)
            .SingleAsync();

        User? accountUser = null;

        if (existingAccount is null)
        {
            var existingUser = !string.IsNullOrEmpty(email)
                ? await db.Queryable<User>().Where(u => u.Email == email).SingleAsync()
                : null;

            if (existingUser is null)
            {
                accountUser = new User { Name = name, Email = email };
                await db.InsertNav(
                        new GitHubOnlineAccount
                        {
                            GitHubLogin = githubLogin,
                            GitHubId = long.Parse(githubId),
                            User = accountUser,
                        }
                    )
                    .Include(a => a.User)
                    .ExecuteCommandAsync();
            }
            else
            {
                accountUser = existingUser;
                await db.Insertable(
                        new GitHubOnlineAccount
                        {
                            GitHubLogin = githubLogin,
                            GitHubId = long.Parse(githubId),
                            UserId = existingUser.Id,
                        }
                    )
                    .ExecuteCommandAsync(ct);
            }
        }
        else
        {
            accountUser =
                existingAccount.User
                ?? await db.Queryable<User>().InSingleAsync(existingAccount.UserId);
        }

        await CookieAuth.SignInAsync(o =>
        {
            o.Claims.AddRange(result.Principal.Claims);
            o.Claims.Add(new Claim(ClaimTypes.NameIdentifier, accountUser.Id.ToString()));

            // Add role claims for ASP.NET/FastEndpoints policy checks
            var adminEmails = adminOptions.Value.Emails.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var adminGitHubLogins = adminOptions.Value.GitHubLogins.ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );

            if (adminEmails.Contains(accountUser.Email) || adminGitHubLogins.Contains(githubLogin))
            {
                o.Claims.Add(new Claim(ClaimTypes.Role, "Root"));
            }
            else
            {
                o.Claims.Add(new Claim(ClaimTypes.Role, "User"));
            }
        });

        await Send.RedirectAsync(options.Value.FrontendUrl, allowRemoteRedirects: true);
    }
}
