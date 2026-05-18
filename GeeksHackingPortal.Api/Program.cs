using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using GeeksHackingPortal.Api;
using GeeksHackingPortal.Api.Authorization;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Data;
using GeeksHackingPortal.Api.DataProtection;
using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Delete;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Options;
using GeeksHackingPortal.Api.Services;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication;
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
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

var serverVersion = new MySqlServerVersion(new Version(8, 4, 6));

builder.Services.AddDbContext<OpenIddictDbContext>(options =>
    options
        .UseMySql(builder.Configuration.GetConnectionString("openiddict"), serverVersion)
        .UseOpenIddict()
);

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

builder
    .Services.AddOpenIddict()
    .AddCore(options => { options.UseEntityFrameworkCore().UseDbContext<OpenIddictDbContext>(); })
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

        var githubOptions = builder.Configuration.GetSection("GitHub").Get<GitHubOptions>();
        if (!string.IsNullOrWhiteSpace(githubOptions?.ClientId)
            && !string.IsNullOrWhiteSpace(githubOptions.ClientSecret))
        {
            options
                .UseWebProviders()
                .AddGitHub(github =>
                {
                    github
                        .AddScopes("user:email")
                        .SetClientId(githubOptions.ClientId)
                        .SetClientSecret(githubOptions.ClientSecret)
                        .SetRedirectUri("/callback/login/github");
                });
        }
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize");
        options.SetEndSessionEndpointUris("connect/logout");
        options.SetIntrospectionEndpointUris("connect/introspect");
        options.SetRevocationEndpointUris("connect/revoke");
        options.SetTokenEndpointUris("connect/token");
        options.SetUserInfoEndpointUris("connect/userinfo");

        options.AllowAuthorizationCodeFlow();
        options.AllowClientCredentialsFlow();
        options.AllowRefreshTokenFlow();

        options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough();
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
        t["Standalone Workshops"] = "Standalone workshop event management";
        t["Challenges"] = "Challenge/track management within hackathons";
        t["Teams"] = "Team creation and management";
        t["Workshops"] = "Hackathon workshop management and attendance";
        t["Registration"] = "Registration questions and responses";
        t["Submissions"] = "Project submission management";
        t["Resources"] = "Event resources and redemption";
        t["Activity Participants"] = "Activity participant management and reviews";
        t["Activity Organizers"] = "Activity organizer membership management";
        t["Judges"] = "Judge management and scoring";
        t["Venue"] = "Venue check-in, check-out, and history";
        t["Timeline"] = "Event timeline management and public timeline views";
        t["Admin"] = "Administrative and operational endpoints";
    };
});
LogStartupPhase("fastendpoints-and-openapi-registered", startupStopwatch, ref startupPhaseTimestamp);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IOAuthApplicationDeletionOperations, OpenIddictOAuthApplicationDeletionOperations>();
builder.Services.AddScoped<OAuthApplicationDeletionService>();

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

        var openIddictDbContext = scope.ServiceProvider.GetRequiredService<OpenIddictDbContext>();
        var pendingMigrations = await openIddictDbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            app.Logger.LogCritical(
                "Database schema validation failed during startup. Run the database migrator workflow before deployment to detect or apply schema changes."
            );
            
            throw new InvalidOperationException(
                "Database schema validation failed during startup. Run the database migrator workflow before deployment to detect or apply schema changes."
            );
        }
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

app.Use(
    async (context, next) =>
    {
        if (IsOpenIdConnectCorsEndpoint(context.Request.Path))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.AccessControlAllowOrigin = "*";
                context.Response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
                context.Response.Headers.AccessControlAllowHeaders =
                    "Accept, Authorization, Content-Type";

                return Task.CompletedTask;
            });

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
        }

        await next();
    }
);

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapMethods(
        "~/connect/authorize",
        [HttpMethods.Get, HttpMethods.Post],
        async (HttpContext httpContext, ISqlSugarClient sql) =>
        {
            var request =
                Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest(
                    httpContext
                )
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var userId = httpContext.User.FindFirst(CustomClaimTypes.UserId)?.Value;
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
            {
                var returnUrl = httpContext.Request.PathBase
                    + httpContext.Request.Path
                    + httpContext.Request.QueryString;

                return Results.Redirect(
                    $"/auth/login?redirect_uri={Uri.EscapeDataString(returnUrl)}"
                );
            }

            var user = await sql.Queryable<User>().Where(u => u.Id == parsedUserId).FirstAsync();
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role
            );

            identity.SetClaim(Claims.Subject, user.Id.ToString());
            identity.SetClaim(Claims.Name, user.Name);
            identity.SetClaim(Claims.GivenName, user.FirstName);
            identity.SetClaim(Claims.FamilyName, user.LastName);
            identity.SetClaim(Claims.Email, user.Email);

            identity.SetDestinations(GetClaimDestinations);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            return Results.SignIn(
                principal,
                authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
        }
    )
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapPost(
        "~/connect/token",
        async (HttpContext httpContext, IOpenIddictApplicationManager applicationManager) =>
        {
            var request =
                Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest(
                    httpContext
                )
                ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {
                var clientId =
                    request.ClientId
                    ?? throw new InvalidOperationException("The client identifier cannot be retrieved.");

                var application =
                    await applicationManager.FindByClientIdAsync(clientId)
                    ?? throw new InvalidOperationException("The application cannot be found.");

                var identity = new ClaimsIdentity(
                    TokenValidationParameters.DefaultAuthenticationType,
                    Claims.Name,
                    Claims.Role
                );

                identity.SetClaim(
                    Claims.Subject,
                    await applicationManager.GetClientIdAsync(application)
                );
                identity.SetClaim(
                    Claims.Name,
                    await applicationManager.GetDisplayNameAsync(application)
                );

                identity.SetDestinations(
                    static claim =>
                        claim.Type switch
                        {
                            Claims.Name
                                when claim.Subject is not null
                                    && claim.Subject.HasScope(Scopes.Profile) =>
                                [Destinations.AccessToken, Destinations.IdentityToken],
                            _ => [Destinations.AccessToken],
                        }
                );

                var principal = new ClaimsPrincipal(identity);
                principal.SetScopes(request.GetScopes());

                return Results.SignIn(
                    principal,
                    authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                );
            }

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
                return Results.Forbid(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]
                );
            {
                var result = await httpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                );
                if (!result.Succeeded || result.Principal is null)
                {
                    return Results.Forbid(
                        authenticationSchemes:
                        [
                            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        ]
                    );
                }

                result.Principal.SetScopes(request.GetScopes());

                foreach (var identity in result.Principal.Identities)
                {
                    identity.SetDestinations(GetClaimDestinations);
                }

                return Results.SignIn(
                    result.Principal,
                    authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                );
            }

        }
    )
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapMethods(
        "~/connect/userinfo",
        [HttpMethods.Get, HttpMethods.Post],
        async (HttpContext httpContext, ISqlSugarClient sql) =>
        {
            var result = await httpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
            if (!result.Succeeded || result.Principal is null)
            {
                return Results.Challenge(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]
                );
            }

            var subject = result.Principal.GetClaim(Claims.Subject);
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Results.Unauthorized();
            }

            var claims = new Dictionary<string, object?> { [Claims.Subject] = subject };

            if (Guid.TryParse(subject, out var userId))
            {
                var user = await sql.Queryable<User>().Where(u => u.Id == userId).FirstAsync();
                if (user is not null)
                {
                    if (result.Principal.HasScope(Scopes.Profile))
                    {
                        claims[Claims.Name] = user.Name;
                        claims[Claims.GivenName] = user.FirstName;
                        claims[Claims.FamilyName] = user.LastName;
                    }

                    if (result.Principal.HasScope(Scopes.Email))
                    {
                        claims[Claims.Email] = user.Email;
                        claims[Claims.EmailVerified] = true;
                    }
                }
            }
            else if (result.Principal.HasScope(Scopes.Profile))
            {
                claims[Claims.Name] = result.Principal.GetClaim(Claims.Name);
            }

            return Results.Ok(claims);
        }
    )
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapMethods(
        "~/connect/logout",
        [HttpMethods.Get, HttpMethods.Post],
        () =>
            Results.SignOut(
                authenticationSchemes:
                [
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                ]
            )
    )
    .AllowAnonymous()
    .ExcludeFromDescription();

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
return;

static bool IsOpenIdConnectCorsEndpoint(PathString path) =>
    path.Equals("/.well-known/openid-configuration")
    || path.Equals("/connect/token")
    || path.Equals("/connect/userinfo")
    || path.Equals("/connect/revoke")
    || path.Equals("/connect/introspect");

static IEnumerable<string> GetClaimDestinations(Claim claim) =>
    claim.Type switch
    {
        Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Name or Claims.GivenName or Claims.FamilyName
            when claim.Subject is not null && claim.Subject.HasScope(Scopes.Profile) =>
            [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Email or Claims.EmailVerified
            when claim.Subject is not null && claim.Subject.HasScope(Scopes.Email) =>
            [Destinations.AccessToken, Destinations.IdentityToken],
        _ => [Destinations.AccessToken],
    };

static void LogStartupPhase(string phase, Stopwatch startupStopwatch, ref long previousTimestamp)
{
    var currentTimestamp = Stopwatch.GetTimestamp();
    var phaseElapsed = Stopwatch.GetElapsedTime(previousTimestamp, currentTimestamp);
    previousTimestamp = currentTimestamp;

    Console.WriteLine(
        $"[startup] {phase} phase={phaseElapsed.TotalMilliseconds:F0}ms total={startupStopwatch.ElapsedMilliseconds}ms"
    );
}
