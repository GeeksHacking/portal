using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

// The WebApp imports @geekshacking/portal-sdk from WebSdk/dist. If nuxt dev starts
// before dist is built, Vite caches the failed import and serves a blank page until
// restarted. So "sdk" reports healthy only once every built entry point exists, and
// "app" waits for that.
var sdkDist = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../GeeksHackingPortal.WebSdk/dist"));
string[] sdkEntryPoints =
[
    "index.js", "hooks.js", "zod.js", "testing.js",
    "testing/handlers.js", "testing/mocks.js", "client/index.js",
];
builder.Services.AddHealthChecks().AddCheck("sdk-dist", () =>
{
    var missing = sdkEntryPoints.Where(f => !File.Exists(Path.Combine(sdkDist, f))).ToArray();
    return missing.Length == 0
        ? HealthCheckResult.Healthy()
        : HealthCheckResult.Unhealthy($"Waiting for the SDK build: {string.Join(", ", missing)}");
});

var sdk = builder
    .AddJavaScriptApp("sdk", "../GeeksHackingPortal.WebSdk")
    .WithPnpm()
    .WithReference(api)
    .WithHealthCheck("sdk-dist")
    .WaitFor(api);

builder
    .AddJavaScriptApp("app", "../GeeksHackingPortal.WebApp")
    .WithPnpm(install: false)
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api)
    .WaitFor(sdk);

builder
    .AddJavaScriptApp("oidc-app-playground", "../GeeksHackingPortal.OidcWebPlayground")
    .WithPnpm(install: false)
    .WithHttpEndpoint(port: 3001, env: "PORT");


builder.Build().Run();
