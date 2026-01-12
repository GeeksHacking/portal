using System.Net.Http.Json;
using TUnit.Core.Interfaces;

namespace HackOMania.Tests.Data;

public class AuthenticatedHttpClientDataClass : IAsyncInitializer, IAsyncDisposable
{
    public HttpClient HttpClient { get; private set; } = new();

    public string GitHubLogin { get; set; } = "qin-guan";
    public long GitHubId { get; set; } = 1;
    public string Name { get; set; } = "Qin Guan";
    public string Email { get; set; } = "qin-guan@outlook.com";

    public async Task InitializeAsync()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        var app = GlobalHooks.App ?? throw new NullReferenceException("App not initialized");
        var baseUri = app.GetEndpoint("api", "https");

        HttpClient = new HttpClient(handler) { BaseAddress = baseUri };

        if (GlobalHooks.NotificationService != null)
        {
            await GlobalHooks
                .NotificationService.WaitForResourceAsync("api", KnownResourceStates.Running)
                .WaitAsync(TimeSpan.FromSeconds(30));
        }

        await AuthenticateAsync();
    }

    private async Task AuthenticateAsync()
    {
        var response = await HttpClient.PostAsJsonAsync(
            "/auth/impersonate",
            new
            {
                GitHubId,
                GitHubLogin,
                Name,
                Email,
            }
        );

        response.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await ValueTask.CompletedTask;
    }
}
