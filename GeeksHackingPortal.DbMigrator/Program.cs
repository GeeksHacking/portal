using ConsoleAppFramework;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.DbMigrator.Commands;
using GeeksHackingPortal.DbMigrator.Commands.OpenIddict;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

var app = ConsoleApp.Create()
    .ConfigureEmptyConfiguration(configure => { configure.AddEnvironmentVariables(); })
    .ConfigureServices((configuration, services) =>
    {
        var connectionString =
            configuration.GetConnectionString("db")
            ?? throw new InvalidOperationException("ConnectionStrings:db is required.");

        var openIddictConnectionString =
            configuration.GetConnectionString("openiddict")
            ?? throw new InvalidOperationException("ConnectionStrings:openiddict is required.");

        services.AddSingleton<ISqlSugarClient>(s =>
        {
            return new SqlSugarScope(
                new ConnectionConfig
                {
                    DbType = DbType.MySql,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,
                    MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true },
                },
                _ => { }
            );
        });

        services.AddDbContext<OpenIddictDbContext>(options =>
            options.UseMySql(openIddictConnectionString, new MySqlServerVersion(new Version(8, 4, 6))));
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    });

app.Add<DiffCommand>();
app.Add<ApplyCommand>();

app.Add<OpenIddictApplyCommand>();

await app.RunAsync(args);