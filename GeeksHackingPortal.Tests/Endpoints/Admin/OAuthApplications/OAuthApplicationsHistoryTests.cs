using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationsHistoryTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task History_ReturnsOkWithItems()
    {
        var clientId = $"test-history-{Guid.NewGuid():N}";
        var createResponse = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            new
            {
                ClientId = clientId,
                DisplayName = "History test application",
                Platform = "Native",
                RedirectUris = new[] { "https://oidc-playground.akamai.com/redirect_uri" },
                PostLogoutRedirectUris = Array.Empty<string>(),
            }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created).IsNotNull();

        var historyResponse = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-applications/{created!.Id}/history"
        );
        var history = await historyResponse.Content.ReadFromJsonAsync<HistoryListResponse>();

        await Assert.That(historyResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.Items).IsNotNull();

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created.Id}");
    }

    [Test]
    public async Task History_NonExistentApplication_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid().ToString();

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-applications/{fakeId}/history"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task History_WithoutAuthentication_ReturnsUnauthorized()
    {
        var fakeId = Guid.NewGuid().ToString();

        var response = await Client.HttpClient.GetAsync(
            $"/admin/oauth-applications/{fakeId}/history"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private sealed class OAuthApplicationResponse
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
    }

    private sealed class HistoryListResponse
    {
        public required IReadOnlyList<HistoryItemResponse> Items { get; init; }
    }

    private sealed class HistoryItemResponse
    {
        public required string Id { get; init; }
        public required string Subject { get; init; }
        public required string UserName { get; init; }
        public required string UserEmail { get; init; }
        public DateTime? CreationDate { get; init; }
        public string? Scopes { get; init; }
    }
}
