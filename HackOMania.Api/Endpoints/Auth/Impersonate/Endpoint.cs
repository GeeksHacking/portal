using System.Security.Claims;
using FastEndpoints;
using FastEndpoints.Security;
using HackOMania.Api.Constants;
using HackOMania.Api.Entities;
using SqlSugar;

namespace HackOMania.Api.Endpoints.Auth.Impersonate;

public class Endpoint(IWebHostEnvironment env, ISqlSugarClient db) : Endpoint<Request>
{
    public override void Configure()
    {
        Post("auth/impersonate");
        AllowAnonymous();
        Description(d => d.ExcludeFromDescription());
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!env.IsDevelopment())
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        var existingAccount = await db.Queryable<GitHubOnlineAccount>()
            .Includes(a => a.User)
            .Where(a => a.GitHubId == req.GitHubId)
            .SingleAsync();

        User accountUser;
        Guid githubAccountId;

        if (existingAccount is null)
        {
            accountUser = new User { Name = req.Name, Email = req.Email };
            var newAccount = new GitHubOnlineAccount
            {
                GitHubLogin = req.GitHubLogin,
                GitHubId = req.GitHubId,
                User = accountUser,
            };
            await db.InsertNav(newAccount).Include(a => a.User).ExecuteCommandAsync();
            githubAccountId = newAccount.Id;
        }
        else
        {
            accountUser = existingAccount.User;
            githubAccountId = existingAccount.Id;
        }

        await CookieAuth.SignInAsync(o =>
        {
            o.Claims.Add(new Claim(ClaimTypes.NameIdentifier, accountUser.Id.ToString()));
            o.Claims.Add(new Claim(CustomClaimTypes.UserId, accountUser.Id.ToString()));
            o.Claims.Add(new Claim(CustomClaimTypes.GitHubAccountId, githubAccountId.ToString()));
        });

        await Send.OkAsync(cancellation: ct);
    }
}
