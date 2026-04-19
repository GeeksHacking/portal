using TUnit.Core.Interfaces;

namespace GeeksHackingPortal.Tests.Data;

public class NoRedirectHttpClientDataClass : IAsyncInitializer, IAsyncDisposable
{
    public HttpClient HttpClient { get; private set; } = new();

    public async Task InitializeAsync()
    {
        var app = GlobalHooks.App ?? throw new NullReferenceException();

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        HttpClient = new HttpClient(handler) { BaseAddress = app.GetEndpoint("api", "https") };

        if (GlobalHooks.NotificationService != null)
        {
            await GlobalHooks
                .NotificationService.WaitForResourceAsync("api", KnownResourceStates.Running)
                .WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await ValueTask.CompletedTask;
    }
}
