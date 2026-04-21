using ConsoleAppFramework;
using GeeksHackingPortal.DbMigrator.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

var app = ConsoleApp.Create()
    .ConfigureEmptyConfiguration(configure =>
    {
        configure.AddEnvironmentVariables();
    })
    .ConfigureServices((configuration, services) =>
    {
        services.AddSingleton<ISqlSugarClient>(s =>
        {
            var connectionString =
                configuration.GetConnectionString("db")
                ?? throw new InvalidOperationException("ConnectionStrings:db is required.");

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
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    });

app.Add<DiffCommand>();
app.Add<ApplyCommand>();

await app.RunAsync(args);
