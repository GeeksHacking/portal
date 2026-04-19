// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly

using Aspire.Hosting;
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

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<GeeksHackingPortal_Api>([
            "UseVolumes=false", // We do not want DB data to be persisted and conflict with local development data
        ]);

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
    }

    [After(TestSession)]
    public static async Task CleanUp()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}
