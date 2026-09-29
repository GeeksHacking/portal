using SqlSugar;

namespace GeeksHackingPortal.Api.Extensions;

public static class SqlSugarLockExtensions
{
    /// <summary>
    /// Takes an exclusive row lock (<c>SELECT ... FOR UPDATE</c>) on the row with the given id.
    /// Must be called inside a transaction; the lock is held until it commits or rolls back.
    /// Use it to serialize check-then-act sequences (capacity, quota and uniqueness checks)
    /// so concurrent requests cannot both pass the check before either writes.
    /// Re-read any state used by the check <em>after</em> acquiring the lock.
    /// </summary>
    /// <returns>True if the row exists.</returns>
    public static async Task<bool> LockRowAsync<T>(this ISqlSugarClient sql, Guid id)
    {
        var ids = await sql.Queryable<T>()
            .Where("Id = @id", new { id })
            .TranLock(DbLockType.Wait)
            .Select<Guid>("Id")
            .ToListAsync();
        return ids.Count > 0;
    }
}
