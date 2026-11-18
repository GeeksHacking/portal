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

// Header image storage: production uses Cloudflare R2; locally we run RustFS, an S3-compatible server.
// The API creates the bucket with a public-read policy on startup, and the browser uploads to it with
// presigned URLs, so the frontend origin must be allowed by CORS.
const string headerImageBucket = "portal";
var storageAccessKey = builder.AddParameter("storage-access-key", "rustfsadmin");
var storageSecretKey = builder.AddParameter("storage-secret-key", "rustfsadmin", secret: true);

var storage = builder
    .AddContainer("storage", "rustfs/rustfs", "1.0.5")
    .WithHttpEndpoint(targetPort: 9000, name: "s3")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithEnvironment("RUSTFS_ACCESS_KEY", storageAccessKey)
    .WithEnvironment("RUSTFS_SECRET_KEY", storageSecretKey)
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
    .WithEnvironment("RUSTFS_CORS_ALLOWED_ORIGINS", appFrontendUrl)
    .WithHttpHealthCheck("/health", endpointName: "s3");
if (builder.Configuration.GetValue("UseVolumes", true))
{
    storage.WithVolume("geekshacking-portal-rustfs", "/data");
}

var storageS3 = storage.GetEndpoint("s3");
api.WithEnvironment("R2:ServiceUrl", storageS3)
    .WithEnvironment(
        "R2:WriteSecrets",
        ReferenceExpression.Create(
            $$"""{"accessKeyId":"{{storageAccessKey}}","secretAccessKey":"{{storageSecretKey}}"}"""
        )
    )
    .WithEnvironment("R2:BucketName", headerImageBucket)
    .WithEnvironment("R2:PublicBaseUrl", ReferenceExpression.Create($"{storageS3}/{headerImageBucket}"))
    .WithEnvironment("R2:EnsureBucket", "true")
    .WaitFor(storage);

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
