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

        GitHubOnlineAccount? existingAccount = null;
        User? accountUser = null;
        var transactionResult = await db.Ado.UseTranAsync(async () =>
        {
            existingAccount = await db.Queryable<GitHubOnlineAccount>()
                .Where(a => a.GitHubId == req.GitHubId)
                .TranLock(DbLockType.Wait)
                .SingleAsync();

            if (existingAccount is not null)
            {
                accountUser = await db.Queryable<User>()
                    .Where(u => u.Id == existingAccount.UserId)
                    .SingleAsync();
                return;
            }

            accountUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = req.FirstName,
                LastName = req.LastName,
                Email = req.Email,
            };
            await db.Insertable(accountUser).ExecuteCommandAsync(ct);

            existingAccount = new GitHubOnlineAccount
            {
                Id = Guid.NewGuid(),
                UserId = accountUser.Id,
                GitHubLogin = req.GitHubLogin,
                GitHubId = req.GitHubId,
            };
            await db.Insertable(existingAccount).ExecuteCommandAsync(ct);
        });

        if (!transactionResult.IsSuccess)
        {
            // If another concurrent request created the account first, recover by reading it.
            existingAccount = await db.Queryable<GitHubOnlineAccount>()
                .Where(a => a.GitHubId == req.GitHubId)
                .SingleAsync();

            if (existingAccount is null)
            {
                throw transactionResult.ErrorException!;
            }

            accountUser = await db.Queryable<User>()
                .Where(u => u.Id == existingAccount.UserId)
                .SingleAsync();

            if (accountUser is null)
            {
                throw transactionResult.ErrorException!;
            }
        }

        if (existingAccount is null || accountUser is null)
        {
            ThrowError("Unable to resolve impersonated account.");
            return;
        }

        var githubAccountId = existingAccount.Id;

        await CookieAuth.SignInAsync(o =>
        {
            o.Claims.Add(new Claim(ClaimTypes.NameIdentifier, accountUser.Id.ToString()));
            o.Claims.Add(new Claim(CustomClaimTypes.UserId, accountUser.Id.ToString()));
            o.Claims.Add(new Claim(CustomClaimTypes.GitHubAccountId, githubAccountId.ToString()));
        });

        await Send.OkAsync(cancellation: ct);
    }
}
