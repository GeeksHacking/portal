using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Storage.V1;
using HackOMania.Api;
using HackOMania.Api.Authorization;
using HackOMania.Api.Converters;
using HackOMania.Api.DataProtection;
using HackOMania.Api.Options;
using HackOMania.Api.Services;
using HackOMania.Api.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Client;
using Scalar.AspNetCore;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Environment.IsProduction())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddGoogle(new LoggingServiceOptions { ProjectId = "hackomania-event-portal" });
    builder.Services.AddGoogleTraceForAspNetCore(
        new AspNetCoreTraceOptions
        {
            ServiceOptions = new TraceServiceOptions { ProjectId = "hackomania-event-portal" },
        }
    );
}

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

builder.Services.AddOptions<AppOptions>().Bind(builder.Configuration.GetSection("App"));
builder.Services.AddOptions<GitHubOptions>().Bind(builder.Configuration.GetSection("GitHub"));
builder.Services.AddOptions<PostmarkOptions>().Bind(builder.Configuration.GetSection("Postmark"));

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();

builder.Services.AddSingleton<ISqlSugarClient>(s =>
{
    return new SqlSugarScope(
        new ConnectionConfig
        {
            DbType = DbType.MySql,
            ConnectionString = builder.Configuration.GetConnectionString("db"),
            IsAutoCloseConnection = true,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                DataInfoCacheService = s.GetRequiredService<ICacheService>(),
                EntityService = (property, column) =>
                {
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (propertyType != typeof(DateTimeOffset))
                    {
                        return;
                    }

                    column.SqlParameterDbType = typeof(DateTimeOffsetUtcConverter);
                },
            },
            MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true },
        },
        db => { }
    );
});

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
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<DbContext>();
    })
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

        options.AddEventHandler<OpenIddictClientEvents.ProcessAuthenticationContext>(builder =>
        {
            builder.UseInlineHandler(context =>
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
        PolicyNames.ParticipantForHackathon,
        policy => policy.Requirements.Add(new ParticipantForHackathonRequirement())
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
        PolicyNames.CreateHackathon,
        policy => policy.Requirements.Add(new CreateHackathonRequirement())
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
                        "https://*.hackomania-event-platform.pages.dev",
                    ]
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains();

        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});
builder.Services.AddFastEndpoints(o => o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All);
builder.Services.SwaggerDocument(options =>
{
    options.EnableJWTBearerAuth = false;
    options.AutoTagPathSegmentIndex = 0; // Disable auto-tagging, we'll use explicit tags
    options.SerializerSettings = settings =>
    {
        settings.Converters.Add(new JsonStringEnumConverter());
    };
    options.DocumentSettings = settings =>
    {
        settings.Title = "HackOMania Event Platform API";
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

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<IJintEvaluationService, JintEvaluationService>();
builder.Services.AddScoped<IEmailService, PostmarkEmailService>();
builder.Services.AddScoped<INotificationTemplateResolver, NotificationTemplateResolver>();
builder.Services.AddScoped<IAuthorizationHandler, RootHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CreateHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizerForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ParticipantForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamMemberForHackathonTeamHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamCreatorForHackathonTeamHandler>();

builder.Services.AddSingleton<DatabaseInitBackgroundService>();

var app = builder.Build();
await app.Services.GetRequiredService<DatabaseInitBackgroundService>().InitializeAsync();

app.UseForwardedHeaders();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c => c.Serializer.Options.Converters.Add(new JsonStringEnumConverter()));
app.UseSwaggerGen(options => options.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference();
app.MapDefaultEndpoints();

app.Run();
