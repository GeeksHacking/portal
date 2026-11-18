using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var appFrontendUrl = builder.AddParameter("app-frontend-url", "http://localhost:3000");

var githubClientId = builder.AddParameter("github-client-id");
var githubClientSecret = builder.AddParameter("github-client-secret");

var mysql = builder.AddMySql("mysql").WithPhpMyAdmin();
if (builder.Configuration.GetValue("UseVolumes", true))
{
    mysql.WithDataVolume("geekshacking-portal-mysql");
}

var db = mysql.AddDatabase("db");
var openIddictDb = mysql.AddDatabase("openiddict");

// S3-compatible object storage for activity banners and files. Production uses Cloudflare R2.
var rustFsAccessKey = builder.AddParameter("rustfs-access-key", "rustfsadmin");
var rustFsSecretKey = builder.AddParameter("rustfs-secret-key", "rustfsadmin", secret: true);
var rustFs = builder
    .AddContainer("fs", "rustfs/rustfs", "latest")
    .WithHttpEndpoint(targetPort: 9000, name: "s3")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithEnvironment("RUSTFS_VOLUMES", "/data/rustfs0")
    .WithEnvironment("RUSTFS_ADDRESS", "0.0.0.0:9000")
    .WithEnvironment("RUSTFS_CONSOLE_ADDRESS", "0.0.0.0:9001")
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
    .WithEnvironment("RUSTFS_CORS_ALLOWED_ORIGINS", "*")
    .WithEnvironment("RUSTFS_CONSOLE_CORS_ALLOWED_ORIGINS", "*")
    .WithEnvironment("RUSTFS_ACCESS_KEY", rustFsAccessKey)
    .WithEnvironment("RUSTFS_SECRET_KEY", rustFsSecretKey)
    .WithEnvironment("RUSTFS_OBS_LOGGER_LEVEL", "info");
if (builder.Configuration.GetValue("UseVolumes", true))
{
    rustFs.WithVolume("geekshacking-portal-fs-data", "/data");
}

var s3 = rustFs.GetEndpoint("s3");

var migrations = builder
    .AddProject<GeeksHackingPortal_DbMigrator>("db-migrator")
    .WithArgs("apply", "--seed-development-template", "--allow-destructive")
    .WithReference(db)
    .WithReference(openIddictDb)
    .WaitFor(db)
    .WaitFor(openIddictDb);

var api = builder
    .AddProject<GeeksHackingPortal_Api>("api")
    .WithReference(db)
    .WithReference(openIddictDb)
    .WithEnvironment("App:FrontendUrl", appFrontendUrl)
    .WithEnvironment("GitHub:ClientId", githubClientId)
    .WithEnvironment("GitHub:ClientSecret", githubClientSecret)
    .WithEnvironment(
        "ConnectionStrings__storage",
        ReferenceExpression.Create(
            $"Endpoint={s3};AccessKey={rustFsAccessKey};SecretKey={rustFsSecretKey};Bucket=geekshacking-portal"
        )
    )
    .WithEnvironment("Storage__CreateBucket", "true")
    .WaitForCompletion(migrations)
    .WaitFor(db)
    .WaitFor(openIddictDb)
    .WaitFor(rustFs);

builder
    .AddJavaScriptApp("sdk", "../GeeksHackingPortal.WebSdk")
    .WithPnpm()
    .WithReference(api)
    .WaitFor(api);

builder
    .AddJavaScriptApp("app", "../GeeksHackingPortal.WebApp")
    .WithPnpm(install: false)
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api);

builder
    .AddJavaScriptApp("oidc-app-playground", "../GeeksHackingPortal.OidcWebPlayground")
    .WithPnpm(install: false)
    .WithHttpEndpoint(port: 3001, env: "PORT");


builder.Build().Run();
