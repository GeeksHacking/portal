using Microsoft.EntityFrameworkCore;

namespace GeeksHackingPortal.Api.Data;

public class OpenIddictDbContext : DbContext
{
    public OpenIddictDbContext(DbContextOptions<OpenIddictDbContext> options)
        : base(options)
    {
    }
}
