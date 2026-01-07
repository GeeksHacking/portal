using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Google.Cloud.Diagnostics.Common;
using HackOMania.Api.Authorization;
using HackOMania.Api.Options;
using HackOMania.Api.Services;
using HackOMania.Api.Workers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddGoogle(new LoggingServiceOptions { ProjectId = "hackomania-event-portal" });
}

builder.Services.AddOptions<AppOptions>().Bind(builder.Configuration.GetSection("App"));
builder.Services.AddOptions<GitHubOptions>().Bind(builder.Configuration.GetSection("GitHub"));

builder.Services.AddSingleton<ISqlSugarClient>(s =>
{
    return new SqlSugarScope(
        new ConnectionConfig
        {
            DbType = DbType.MySql,
            ConnectionString = builder.Configuration.GetConnectionString("db"),
            IsAutoCloseConnection = true,
        },
        db => { }
    );
});

builder.Services.AddDbContext<DbContext>(options =>
{
    options.UseInMemoryDatabase("Db");
    options.UseOpenIddict();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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
        options.UseAspNetCore().EnableRedirectionEndpointPassthrough();
        options.UseSystemNetHttp();

        options
            .UseWebProviders()
            .AddGitHub(github =>
            {
                var githubOptions = builder
                    .Configuration.GetSection("GitHub")
                    .Get<GitHubOptions>()!;

                github
                    .AddScopes("email", "profile")
                    .SetClientId(githubOptions.ClientId)
                    .SetClientSecret(githubOptions.ClientSecret)
                    .SetRedirectUri("callback/login/github");
            });
    });

builder
    .Services.AddAuthenticationCookie(validFor: TimeSpan.FromDays(7))
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
        PolicyNames.CreateHackathon,
        policy => policy.Requirements.Add(new CreateHackathonRequirement())
    );

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            builder.Environment.IsDevelopment()
                ? ["http://localhost:3000"]
                : ["https://hackomania.geekshacking.org"]
        );

        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(options =>
{
    options.EnableJWTBearerAuth = false;
    options.AutoTagPathSegmentIndex = 0; // Disable auto-tagging, we'll use explicit tags
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
        t["Hackathons"] = "Hackathon event management";
        t["Challenges"] = "Challenge/track management within hackathons";
        t["Teams"] = "Team creation and management";
        t["Submissions"] = "Project submission management";
        t["Resources"] = "Event resources and redemption";
        t["Participants"] = "Participant management and reviews";
        t["Organizers"] = "Organizer management";
        t["Judges"] = "Judge management and scoring";
    };
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<IAuthorizationHandler, RootHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CreateHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizerForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ParticipantForHackathonHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamMemberForHackathonTeamHandler>();

builder.Services.AddHostedService<DatabaseInitBackgroundService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();
app.UseSwaggerGen(options => options.Path = "/openapi/{documentName}.json");
app.MapScalarApiReference();

app.Run();
