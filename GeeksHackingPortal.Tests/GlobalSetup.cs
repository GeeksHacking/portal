// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly

using Aspire.Hosting;
using GeeksHackingPortal.Tests.Helpers;
using Projects;
using System.Diagnostics.CodeAnalysis;

[assembly: Retry(3)]
[assembly: ExcludeFromCodeCoverage]

namespace GeeksHackingPortal.Tests;

public class GlobalHooks
{
    public static DistributedApplication? App { get; private set; }
    public static ResourceNotificationService? NotificationService { get; private set; }
    public static HttpClient? ApiClient { get; private set; }
    public static ServerLogCollector? ServerLogs { get; private set; }

    [Before(TestSession)]
    public static async Task SetUp()
    {
        // Set environment variables for the test process before creating the AppHost
        // These will be picked up by the Aspire parameter configuration
        Environment.SetEnvironmentVariable(
            "Parameters__github-client-id",
            Environment.GetEnvironmentVariable("TEST_GITHUB_CLIENT_ID") ?? "test-client-id"
        );
        Environment.SetEnvironmentVariable(
            "Parameters__github-client-secret",
            Environment.GetEnvironmentVariable("TEST_GITHUB_CLIENT_SECRET") ?? "test-client-secret"
        );
        Environment.SetEnvironmentVariable("Parameters__app-frontend-url", "http://localhost:3000");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<GeeksHackingPortal_AppHost>([
            "UseVolumes=false", // We do not want DB data to be persisted and conflict with local development data
        ]);

        // Production runs with a non-UTC host time zone. Mirror it so that timestamps are verified to be stored
        // and returned in UTC regardless of the host's local time zone.
        foreach (var resourceName in new[] { "api", "db-migrator" })
        {
            appHost.CreateResourceBuilder<ProjectResource>(resourceName).WithEnvironment("TZ", "Asia/Singapore");
        }

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                }
            );
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await appHost.BuildAsync();
        NotificationService = App.Services.GetRequiredService<ResourceNotificationService>();
        await App.StartAsync();

        await NotificationService
            .WaitForResourceAsync("api", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(60));

        ApiClient = App.CreateHttpClient("api", "https");

        ServerLogs = new ServerLogCollector();
        ServerLogs.Start(App.Services.GetRequiredService<ResourceLoggerService>(), "api");
    }

    [BeforeEvery(Test)]
    public static void MarkServerLogStart(TestContext context) => ServerLogs?.MarkTestStart(context);

    /// <summary>
    /// Adds server-side warnings, errors and stack traces logged while the test ran to the test's output,
    /// so failures such as HTTP 500 responses show their cause in the test report.
    /// </summary>
    [AfterEvery(Test)]
    public static void AttachServerLogs(TestContext context)
    {
        var logs = ServerLogs?.GetInterestingSince(context);
        if (!string.IsNullOrWhiteSpace(logs))
        {
            context.Output.WriteLine("--- Server log (warnings/errors during this test; may include concurrent tests) ---");
            context.Output.WriteLine(logs);
        }
    }

    [After(TestSession)]
    public static async Task CleanUp()
    {
        if (ServerLogs is not null)
        {
            // Give the log stream a moment to flush the final lines before snapshotting.
            await Task.Delay(TimeSpan.FromSeconds(1));
            var directory = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(directory);
            ServerLogs.WriteToFile(Path.Combine(directory, "api-server.log"));
            await ServerLogs.DisposeAsync();
        }

        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}
