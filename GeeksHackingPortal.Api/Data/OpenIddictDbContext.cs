using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace GeeksHackingPortal.Api.Data;

public class OpenIddictDbContext : DbContext
{
    public OpenIddictDbContext(DbContextOptions<OpenIddictDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OpenIddictEntityFrameworkCoreToken>(token =>
        {
            token
                .HasIndex(x => new
                {
                    x.ApplicationId,
                    x.Status,
                    x.Subject,
                    x.Type,
                })
                .HasDatabaseName(OpenIddictMySqlIndexConfiguration.TokenAppStatusSubjectTypeIndexName)
                .HasAnnotation(
                    "MySql:IndexPrefixLength",
                    OpenIddictMySqlIndexConfiguration.TokenAppStatusSubjectTypePrefixLengths
                );
        });
    }
}
