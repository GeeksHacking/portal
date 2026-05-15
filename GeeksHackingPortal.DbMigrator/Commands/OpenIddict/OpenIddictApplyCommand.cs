using ConsoleAppFramework;
using GeeksHackingPortal.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeeksHackingPortal.DbMigrator.Commands.OpenIddict;

public class OpenIddictApplyCommand(ILogger<OpenIddictApplyCommand> logger, OpenIddictDbContext dbContext)
{
    /// <summary>
    /// Apply pending OpenIddictDbContext migrations
    /// </summary>
    /// <param name="cancellationToken"></param>
    [Command("openiddict apply")]
    public async Task Apply(
        CancellationToken cancellationToken = default
    )
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation(
            "Database migration applied successfully."
        );
    }
}
