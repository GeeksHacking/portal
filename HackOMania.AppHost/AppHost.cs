using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var appFrontendUrl = builder.AddParameter("app-frontend-url", "http://localhost:3000");

var githubClientId = builder.AddParameter("github-client-id");
var githubClientSecret = builder.AddParameter("github-client-secret");

var mysql = builder.AddMySql("mysql").WithPhpMyAdmin();
if (builder.Configuration.GetValue("UseVolumes", true))
{
    mysql.WithDataVolume("hackomania-mysql");
}

var db = mysql.AddDatabase("db");
#pragma warning disable ASPIRECERTIFICATES001
var cache = builder.AddRedis("cache").WithRedisInsight().WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001

var api = builder
    .AddProject<HackOMania_Api>("api")
    .WithReference(db)
    .WithReference(cache)
    .WithEnvironment("App:FrontendUrl", appFrontendUrl)
    .WithEnvironment("GitHub:ClientId", githubClientId)
    .WithEnvironment("GitHub:ClientSecret", githubClientSecret)
    .WaitFor(db)
    .WaitFor(cache);

builder
    .AddJavaScriptApp("app", "../HackOMania.WebApp")
    .WithPnpm()
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api);

builder.Build().Run();
