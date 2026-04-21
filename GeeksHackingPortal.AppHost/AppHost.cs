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

var migrations = builder
    .AddProject<GeeksHackingPortal_DbMigrator>("db-migrator")
    .WithArgs("apply", "--seed-development-template")
    .WithReference(db)
    .WaitFor(db);

var api = builder
    .AddProject<GeeksHackingPortal_Api>("api")
    .WithReference(db)
    .WithEnvironment("App:FrontendUrl", appFrontendUrl)
    .WithEnvironment("GitHub:ClientId", githubClientId)
    .WithEnvironment("GitHub:ClientSecret", githubClientSecret)
    .WaitForCompletion(migrations)
    .WaitFor(db);

builder
    .AddJavaScriptApp("app", "../GeeksHackingPortal.WebApp")
    .WithPnpm()
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api);

builder.Build().Run();
