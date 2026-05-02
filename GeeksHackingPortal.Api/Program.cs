using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using GeeksHackingPortal.Api;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.DataProtection;
using GeeksHackingPortal.Api.Options;
using GeeksHackingPortal.Api.Services;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Client;
using Scalar.AspNetCore;
using SqlSugar;
using System.Diagnostics;
using System.Text.Json.Serialization;

var startupStopwatch = Stopwatch.StartNew();
var startupPhaseTimestamp = Stopwatch.GetTimestamp();
LogStartupPhase("process-started", startupStopwatch, ref startupPhaseTimestamp);

var builder = WebApplication.CreateBuilder(args);
LogStartupPhase("builder-created", startupStopwatch, ref startupPhaseTimestamp);

builder.AddServiceDefaults();
LogStartupPhase("service-defaults-registered", startupStopwatch, ref startupPhaseTimestamp);

if (builder.Environment.IsProduction())
{
    builder.Logging.AddGoogle(new LoggingServiceOptions { ProjectId = "hackomania-event-portal" }); // Legacy project ID
    builder.Services.AddGoogleTraceForAspNetCore(
        new AspNetCoreTraceOptions
        {
            ServiceOptions = new TraceServiceOptions { ProjectId = "hackomania-event-portal" }, // Legacy project ID
        }
    );
}
LogStartupPhase("cloud-diagnostics-configured", startupStopwatch, ref startupPhaseTimestamp);

var dataProtectionBuilder = builder
    .Services.AddDataProtection()
    .SetApplicationName(builder.Environment.ApplicationName);
var dataProtectionBucketName = builder.Configuration["DataProtection:BucketName"];
if (!string.IsNullOrWhiteSpace(dataProtectionBucketName))
{
    var dataProtectionKeyPrefix =
        builder.Configuration["DataProtection:KeyPrefix"] ?? "data-protection";
    dataProtectionBuilder.AddKeyManagementOptions(options =>
    {
        options.XmlRepository = new GoogleCloudStorageXmlRepository(
            StorageClient.Create(),
            dataProtectionBucketName,
            dataProtectionKeyPrefix
        );
    });
}
LogStartupPhase("data-protection-configured", startupStopwatch, ref startupPhaseTimestamp);

builder.Services.AddOptions<AppOptions>().Bind(builder.Configuration.GetSection("App"));
builder.Services.AddOptions<GitHubOptions>().Bind(builder.Configuration.GetSection("GitHub"));
builder.Services.AddOptions<PostmarkOptions>().Bind(builder.Configuration.GetSection("Postmark"));

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

builder.Services.AddSingleton<ISqlSugarClient>(s =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("db")
        ?? throw new InvalidOperationException("ConnectionStrings:db is required.");

    return new SqlSugarScope(
        new ConnectionConfig
        {
            DbType = DbType.MySql,
            ConnectionString = connectionString,
            IsAutoCloseConnection = true,
            MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true },
            ConfigureExternalServices = new ConfigureExternalServices
            {
                DataInfoCacheService = s.GetRequiredService<ICacheService>(),
            },
        },
        _ => { }
    );
});
LogStartupPhase("database-client-registered", startupStopwatch, ref startupPhaseTimestamp);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHttpClient();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<DbContext>(options =>
{
    options.UseInMemoryDatabase("openiddict");
    options.UseOpenIddict();
});

builder
    .Services.AddOpenIddict()
    .AddCore(options => { options.UseEntityFrameworkCore().UseDbContext<DbContext>(); })
    .AddClient(options =>
    {
        options.AllowAuthorizationCodeFlow();
        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        var aspNetOptions = options
            .UseAspNetCore()
            .EnableRedirectionEndpointPassthrough()
            .EnableErrorPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            aspNetOptions.DisableTransportSecurityRequirement();
        }

        options.UseSystemNetHttp();
        options.SetRedirectionEndpointUris("/callback/login/github");

        options.AddEventHandler<OpenIddictClientEvents.ProcessAuthenticationContext>(builder2 =>
        {
            builder2.UseInlineHandler(context =>
            {
                var properties = context.Properties;
                if (properties is null)
                {
                    return default;
                }

                var accessToken =
                    context.BackchannelAccessToken
                    ?? context.FrontchannelAccessToken
                    ?? context.IssuedToken;
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    properties["access_token"] = accessToken;
                }

                if (!string.IsNullOrWhiteSpace(context.RefreshToken))
                {
                    properties["refresh_token"] = context.RefreshToken;
                }

                return default;
            });
        });

        options
            .UseWebProviders()
            .AddGitHub(github =>
            {
                var githubOptions = builder
                    .Configuration.GetSection("GitHub")
                    .Get<GitHubOptions>()!;

                github
                    .AddScopes("user:email")
                    .SetClientId(githubOptions.ClientId)
                    .SetClientSecret(githubOptions.ClientSecret)
                    .SetRedirectUri("/callback/login/github");
            });
    });

builder
    .Services.AddAuthenticationCookie(
        validFor: TimeSpan.FromDays(7),
        options =>
        {
            if (builder.Environment.IsProduction())
            {
                options.Cookie.Domain = ".geekshacking.com";
                options.Cookie.SameSite = SameSiteMode.Lax;
            }
            else
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            }
        }
    )
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.Root, policy => policy.Requirements.Add(new RootRequirement()))
    .AddPolicy(
        PolicyNames.OrganizerForHackathon,
        policy => policy.Requirements.Add(new OrganizerForHackathonRequirement())
    )
    .AddPolicy(
        PolicyNames.OrganizerForActivity,
        policy => policy.Requirements.Add(new OrganizerForActivityRequirement())
    )
    .AddPolicy(
        PolicyNames.ParticipantForHackathon,
        policy => policy.Requirements.Add(new ParticipantForHackathonRequirement())
    )
    .AddPolicy(
        PolicyNames.ParticipantForActivity,
        policy => policy.Requirements.Add(new ParticipantForActivityRequirement())
    )
    .AddPolicy(
        PolicyNames.TeamMemberForHackathonTeam,
        policy => policy.Requirements.Add(new TeamMemberForHackathonTeamRequirement())
    )
    .AddPolicy(
        PolicyNames.TeamCreatorForHackathonTeam,
        policy => policy.Requirements.Add(new TeamCreatorForHackathonTeamRequirement())
    )
    .AddPolicy(
        PolicyNames.CreateActivity,
        policy => policy.Requirements.Add(new CreateActivityRequirement())
    );

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                builder.Environment.IsDevelopment()
                    ? ["http://localhost:3000", "https://localhost:3000"]
                    :
                    [
                        "https://portal.geekshacking.com",
                        "https://portal.dev-d73.workers.dev",
                        "https://*-portal.dev-d73.workers.dev"
                    ]
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains();

        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});
LogStartupPhase("platform-services-registered", startupStopwatch, ref startupPhaseTimestamp);

builder.Services.AddFastEndpoints(o => o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All);
builder.Services.SwaggerDocument(options =>
{
    options.EnableJWTBearerAuth = false;
    options.AutoTagPathSegmentIndex = 0; // Disable auto-tagging, we'll use explicit tags
    options.SerializerSettings = settings => { settings.Converters.Add(new JsonStringEnumConverter()); };
    options.DocumentSettings = settings =>
    {
        settings.Title = "GeeksHacking Portal API";
        settings.DocumentName = "v1";
        settings.Description =
            "API for managing hackathon events, teams, challenges, and submissions";
    };
    options.TagDescriptions = t =>
    {
        t["Auth"] = "Authentication and user identity endpoints";
        t["Users"] = "User profile management";
        t["Hackathons"] = "Hackathon event management";
        t["Challenges"] = "Challenge/track management within hackathons";
        t["Teams"] = "Team creation and management";
        t["Submissions"] = "Project submission management";
        t["Resources"] = "Event resources and redemption";
        t["Participants"] = "Participant management and reviews";
        t["Organizers"] = "Organizer management";
        t["Judges"] = "Judge management and scoring";
        t["Admin"] = "Administrative and operational endpoints";
    };
});
LogStartupPhase("fastendpoints-and-openapi-registered", startupStopwatch, ref startupPhaseTimestamp);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<IGitHubRepositoryAutomationService, GitHubRepositoryAutomationService>();
builder.Services.AddScoped<IJintEvaluationService, JintEvaluationService>();
builder.Services.AddScoped<IEmailService, PostmarkEmailService>();
builder.Services.AddScoped<INotificationTemplateResolver, NotificationTemplateResolver>();
builder.Services.AddScoped<IAuthorizationHandler, RootHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CreateActivityHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizerForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizerForActivityHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ParticipantForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ParticipantForActivityHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamMemberForHackathonTeamHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamCreatorForHackathonTeamHandler>();

await using var app = builder.Build();
LogStartupPhase("application-built", startupStopwatch, ref startupPhaseTimestamp);

var validateDatabaseSchema = builder.Configuration.GetValue(
    "Startup:ValidateDatabaseSchema",
    !builder.Environment.IsProduction()
);

if (validateDatabaseSchema)
{
    var schemaMismatchLogged = false;
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sql = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var schemaReport = SchemaDifferenceInspector.Inspect(sql);

        if (schemaReport.HasDifferences)
        {
            app.Logger.LogCritical(
                "Database schema does not match the current SqlSugar models. Apply the pending changes before starting the API."
            );
            SchemaDifferenceLogger.Write(app.Logger, schemaReport, LogLevel.Critical);
            schemaMismatchLogged = true;

            throw new InvalidOperationException(
                "Database schema does not match the current SqlSugar models. Review the diff and apply the schema changes before starting the API."
            );
        }

        app.Logger.LogInformation(
            "Database schema validation passed for {EntityCount} entities.",
            schemaReport.EntityTypes.Count
        );
    }
    catch (Exception exception) when (!schemaMismatchLogged)
    {
        app.Logger.LogCritical(exception, "Database schema validation failed during startup.");
        throw;
    }

    LogStartupPhase("database-schema-validated", startupStopwatch, ref startupPhaseTimestamp);
}
else
{
    app.Logger.LogInformation(
        "Database schema validation skipped during startup. Run the database migrator workflow before deployment to detect or apply schema changes."
    );
    LogStartupPhase("database-schema-validation-skipped", startupStopwatch, ref startupPhaseTimestamp);
}

app.UseForwardedHeaders();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
    c.Endpoints.AllowEmptyRequestDtos = true;
});
app.UseSwaggerGen(options => options.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference();
app.MapDefaultEndpoints();
LogStartupPhase("request-pipeline-configured", startupStopwatch, ref startupPhaseTimestamp);

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation(
        "API startup completed in {ElapsedMilliseconds} ms.",
        startupStopwatch.ElapsedMilliseconds
    )
);

await app.RunAsync();

static void LogStartupPhase(string phase, Stopwatch startupStopwatch, ref long previousTimestamp)
{
    var currentTimestamp = Stopwatch.GetTimestamp();
    var phaseElapsed = Stopwatch.GetElapsedTime(previousTimestamp, currentTimestamp);
    previousTimestamp = currentTimestamp;

    Console.WriteLine(
        $"[startup] {phase} phase={phaseElapsed.TotalMilliseconds:F0}ms total={startupStopwatch.ElapsedMilliseconds}ms"
    );
}
