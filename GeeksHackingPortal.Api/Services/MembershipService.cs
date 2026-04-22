using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Options;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace GeeksHackingPortal.Api.Services;

public class MembershipService(ISqlSugarClient sql, IOptions<AppOptions> appOptions)
{
    private const int CacheSeconds = 60;
    private readonly HashSet<string> _adminEmails = appOptions.Value.AdminEmails.ToHashSet(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly HashSet<string> _adminGitHubLogins =
        appOptions.Value.AdminGitHubLogins.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<User?> GetUser(Guid userId, CancellationToken ct = default)
    {
        return await sql.Queryable<User>().WithCache().InSingleAsync(userId);
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
            .WithCache()
            .Where(a => a.UserId == userId)
            .FirstAsync(ct);
        return githubAccount is not null && _adminGitHubLogins.Contains(githubAccount.GitHubLogin);
    }

    private bool IsAdminByGitHubLogin(User user)
    {
        var githubAccount = sql.Queryable<GitHubOnlineAccount>()
            .WithCache()
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

    public async Task<bool> IsActivityOrganizer(
        Guid userId,
        Guid activityId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<ActivityOrganizer>()
            .AnyAsync(o => o.UserId == userId && o.ActivityId == activityId, ct);
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

    public async Task<bool> IsActivityOrganizerOrRoot(
        Guid userId,
        Guid activityId,
        CancellationToken ct = default
    )
    {
        if (await IsRoot(userId, ct))
        {
            return true;
        }

        return await IsActivityOrganizer(userId, activityId, ct);
    }

    public async Task<bool> IsAnyOrganizerOrRoot(Guid userId, CancellationToken ct = default)
    {
        if (await IsRoot(userId, ct))
        {
            return true;
        }

        return await sql.Queryable<Organizer>().AnyAsync(o => o.UserId == userId, ct);
    }

    public async Task<Participant?> GetParticipant(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Participant>()
            .Where(p => p.UserId == userId && p.HackathonId == hackathonId && p.WithdrawnAt == null)
            .FirstAsync(ct);
    }

    public async Task<ActivityRegistration?> GetActivityRegistration(
        Guid userId,
        Guid activityId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<ActivityRegistration>()
            .Where(r =>
                r.UserId == userId
                && r.ActivityId == activityId
                && r.Status == ActivityRegistrationStatus.Registered
                && r.WithdrawnAt == null
            )
            .FirstAsync(ct);
    }

    public async Task<bool> IsParticipant(
        Guid userId,
        Guid hackathonId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<Participant>()
            .AnyAsync(p => p.UserId == userId && p.HackathonId == hackathonId && p.WithdrawnAt == null, ct);
    }

    public async Task<bool> IsActivityRegistered(
        Guid userId,
        Guid activityId,
        CancellationToken ct = default
    )
    {
        return await sql.Queryable<ActivityRegistration>()
            .AnyAsync(
                r =>
                    r.UserId == userId
                    && r.ActivityId == activityId
                    && r.Status == ActivityRegistrationStatus.Registered
                    && r.WithdrawnAt == null,
                ct
            );
    }
}
