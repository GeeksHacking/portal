using HackOMania.Api.Entities;
using HackOMania.Api.Options;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace HackOMania.Api.Services;

public class MembershipService(ISqlSugarClient sql, IOptions<AdminOptions> adminOptions)
{
    private readonly HashSet<string> _adminEmails = adminOptions.Value.Emails.ToHashSet(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly HashSet<string> _adminGitHubLogins = adminOptions.Value.GitHubLogins.ToHashSet(
        StringComparer.OrdinalIgnoreCase
    );

    public async Task<User?> GetUser(Guid userId, CancellationToken ct = default)
    {
        return await sql.Queryable<User>().InSingleAsync(userId);
    }

    public async Task<bool> IsRoot(Guid userId, CancellationToken ct = default)
    {
        var user = await GetUser(userId, ct);
        return user is not null && IsAdmin(user);
    }

    public bool IsAdmin(User user)
    {
        return _adminEmails.Contains(user.Email) || IsAdminByGitHubLogin(user);
    }

    public async Task<bool> IsAdminByGitHubLogin(Guid userId, CancellationToken ct = default)
    {
        var githubAccount = await sql.Queryable<GitHubOnlineAccount>()
            .Where(a => a.UserId == userId)
            .FirstAsync(ct);
        return githubAccount is not null && _adminGitHubLogins.Contains(githubAccount.GitHubLogin);
    }

    private bool IsAdminByGitHubLogin(User user)
    {
        var githubAccount = sql.Queryable<GitHubOnlineAccount>()
            .Where(a => a.UserId == user.Id)
            .First();
        return githubAccount is not null && _adminGitHubLogins.Contains(githubAccount.GitHubLogin);
    }

    public async Task<bool> IsOrganizer(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Organizer>()
            .AnyAsync(o => o.UserId == userId && o.HackathonId == hackathonId, ct);
    }

    public async Task<bool> IsOrganizerOrRoot(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        if (await IsRoot(userId, ct))
        {
            return true;
        }

        return await IsOrganizer(userId, hackathonId, ct);
    }

    public async Task<Participant?> GetParticipant(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Participant>()
            .Where(p => p.UserId == userId && p.HackathonId == hackathonId)
            .FirstAsync(ct);
    }

    public async Task<bool> IsParticipant(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Participant>()
            .AnyAsync(p => p.UserId == userId && p.HackathonId == hackathonId, ct);
    }

    public async Task<Hackathon?> FindHackathon(Guid idOrShortCode, CancellationToken ct = default)
    {
        return await FindHackathon(idOrShortCode.ToString(), ct);
    }

    public async Task<Hackathon?> FindHackathon(
        string idOrShortCode,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Hackathon>()
            .Where(h => h.Id.ToString() == idOrShortCode || h.ShortCode == idOrShortCode)
            .FirstAsync(ct);
    }
}
