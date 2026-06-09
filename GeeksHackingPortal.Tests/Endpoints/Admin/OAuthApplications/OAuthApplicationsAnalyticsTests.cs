using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationsAnalyticsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task Analytics_ReturnsOkWithItems()
    {
        var clientId = $"test-analytics-{Guid.NewGuid():N}";
        var createResponse = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            new
            {
                ClientId = clientId,
                DisplayName = "Analytics test application",
                Platform = "Native",
                RedirectUris = new[] { "https://oidc-playground.akamai.com/redirect_uri" },
                PostLogoutRedirectUris = Array.Empty<string>(),
            }
        );
        var created = await createResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created).IsNotNull();

        var analyticsResponse = await AuthenticatedClient.HttpClient.GetAsync(
            "/admin/oauth-applications/analytics"
        );
        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<AnalyticsListResponse>();

        await Assert.That(analyticsResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(analytics).IsNotNull();
        await Assert.That(analytics!.Items).IsNotNull();
        await Assert.That(analytics.Items.Any(a => a.ApplicationId == created!.Id)).IsTrue();

        var item = analytics.Items.First(a => a.ApplicationId == created!.Id);
        await Assert.That(item.DisplayName).IsEqualTo("Analytics test application");
        await Assert.That(item.TotalAuthorizations).IsGreaterThanOrEqualTo(0);
        await Assert.That(item.UniqueUsers).IsGreaterThanOrEqualTo(0);

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created!.Id}");
    }

    [Test]
    public async Task Analytics_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.GetAsync("/admin/oauth-applications/analytics");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private sealed class OAuthApplicationResponse
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
    }

    private sealed class AnalyticsListResponse
    {
        public required IReadOnlyList<AnalyticsItemResponse> Items { get; init; }
    }

    private sealed class AnalyticsItemResponse
    {
        public required string ApplicationId { get; init; }
        public required string DisplayName { get; init; }
        public int TotalAuthorizations { get; init; }
        public int UniqueUsers { get; init; }
    }
}
