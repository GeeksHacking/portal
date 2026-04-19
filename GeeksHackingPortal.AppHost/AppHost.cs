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

var api = builder
    .AddProject<GeeksHackingPortal_Api>("api")
    .WithReference(db)
    .WithEnvironment("App:FrontendUrl", appFrontendUrl)
    .WithEnvironment("GitHub:ClientId", githubClientId)
    .WithEnvironment("GitHub:ClientSecret", githubClientSecret)
    .WaitFor(db);

builder
    .AddJavaScriptApp("app", "../GeeksHackingPortal.WebApp")
    .WithPnpm()
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api);

builder.Build().Run();
