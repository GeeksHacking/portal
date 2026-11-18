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
    .WaitForCompletion(migrations)
    .WaitFor(db)
    .WaitFor(openIddictDb);

// Cloudflare R2 header image storage is optional locally: only wired up when
// the r2 bucket name parameter is set (env var Parameters__r2_bucket_name, or user secrets).
// Without it, header image uploads return 503.
if (
    !string.IsNullOrWhiteSpace(
        builder.Configuration["Parameters:r2-bucket-name"] ?? builder.Configuration["Parameters:r2_bucket_name"]
    )
)
{
    api.WithEnvironment("R2:AccountId", builder.AddParameter("r2-account-id"))
        .WithEnvironment("R2:AccessKeyId", builder.AddParameter("r2-access-key-id", secret: true))
        .WithEnvironment("R2:SecretAccessKey", builder.AddParameter("r2-secret-access-key", secret: true))
        .WithEnvironment("R2:BucketName", builder.AddParameter("r2-bucket-name"))
        .WithEnvironment("R2:PublicBaseUrl", builder.AddParameter("r2-public-base-url"));
}

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
