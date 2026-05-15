using ConsoleAppFramework;
using GeeksHackingPortal.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeeksHackingPortal.DbMigrator.Commands.OpenIddict;

public class OpenIddictDiffCommand(ILogger<OpenIddictDiffCommand> logger, OpenIddictDbContext dbContext)
{
    /// <summary>
    /// Inspect pending OpenIddictDbContext pending migrations
    /// </summary>
    [Command("openiddict diff")]
    public Task<int> Diff(CancellationToken cancellationToken)
    {
        logger.LogInformation("Inspecting pending OpenIddict migrations");
        cancellationToken.ThrowIfCancellationRequested();

        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToArray();
        var hasPendingMigrations = pendingMigrations.Length > 0;

        if (hasPendingMigrations)
        {
            logger.LogWarning(
                "Pending OpenIddict migrations detected: {PendingMigrations}",
                string.Join(", ", pendingMigrations)
            );
        }
        else
        {
            logger.LogInformation("No pending OpenIddict migrations detected.");
        }

        return Task.FromResult(hasPendingMigrations ? 2 : 0);
    }
}
