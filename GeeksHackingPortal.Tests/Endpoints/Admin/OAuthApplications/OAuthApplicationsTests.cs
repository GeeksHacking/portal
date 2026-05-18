using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task CreateNativeApplication_ReturnsPublicClientWithoutSecret()
    {
        var clientId = CreateClientId("native");
        var request = CreateRequest(
            clientId,
            "Native OAuth test application",
            "Native",
            ["https://oidc-playground.akamai.com/redirect_uri"]
        );

        var response = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ClientId).IsEqualTo(clientId);
        await Assert.That(result.Platform).IsEqualTo("Native");
        await Assert.That(result.ClientSecret).IsNull();
        await Assert.That(result.RedirectUris.Single()).IsEqualTo(request.RedirectUris.Single());

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{result.Id}");
    }

    [Test]
    public async Task CreateWebApplication_ReturnsClientSecretOnce()
    {
        var clientId = CreateClientId("web");
        var request = CreateRequest(
            clientId,
            "Web OAuth test application",
            "Web",
            ["https://example.com/callback"],
            ["https://example.com/signed-out"]
        );

        var createResponse = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            request
        );
        var createResult = await createResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(createResult).IsNotNull();
        await Assert.That(createResult!.ClientId).IsEqualTo(clientId);
        await Assert.That(createResult.Platform).IsEqualTo("Web");
        await Assert.That(createResult.ClientSecret).IsNotNull();
        await Assert.That(createResult.PostLogoutRedirectUris.Single())
            .IsEqualTo(request.PostLogoutRedirectUris.Single());

        var getResponse = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-applications/{createResult.Id}"
        );
        var getResult = await getResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(getResult).IsNotNull();
        await Assert.That(getResult!.ClientId).IsEqualTo(clientId);
        await Assert.That(getResult.ClientSecret).IsNull();

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{createResult.Id}");
    }

    [Test]
    public async Task UpdateApplication_DoesNotRequireIdInBody()
    {
        var clientId = CreateClientId("update");
        var createResponse = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            CreateRequest(
                clientId,
                "Application before update",
                "Native",
                ["https://oidc-playground.akamai.com/redirect_uri"]
            )
        );
        var created = await createResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created).IsNotNull();

        var updateRequest = CreateRequest(
            clientId,
            "Application after update",
            "Native",
            ["https://oidc-playground.akamai.com/redirect_uri", "http://localhost/callback"]
        );
        var updateResponse = await AuthenticatedClient.HttpClient.PutAsJsonAsync(
            $"/admin/oauth-applications/{created!.Id}",
            updateRequest
        );
        var updated = await updateResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Id).IsEqualTo(created.Id);
        await Assert.That(updated.DisplayName).IsEqualTo(updateRequest.DisplayName);
        await Assert.That(updated.RedirectUris.Count).IsEqualTo(2);

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created.Id}");
    }

    [Test]
    public async Task ListAndDeleteApplication_ReturnsOnlyLiveOwnedApplications()
    {
        var clientId = CreateClientId("delete");
        var createResponse = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            CreateRequest(
                clientId,
                "Application to delete",
                "Native",
                ["https://oidc-playground.akamai.com/redirect_uri"]
            )
        );
        var created = await createResponse.Content.ReadFromJsonAsync<OAuthApplicationResponse>();

        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created).IsNotNull();

        var listBeforeResponse = await AuthenticatedClient.HttpClient.GetAsync(
            "/admin/oauth-applications"
        );
        var listBefore = await listBeforeResponse.Content.ReadFromJsonAsync<OAuthApplicationListResponse>();

        await Assert.That(listBeforeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(listBefore).IsNotNull();
        await Assert.That(listBefore!.Items.Any(item => item.Id == created!.Id)).IsTrue();

        var deleteResponse = await AuthenticatedClient.HttpClient.DeleteAsync(
            $"/admin/oauth-applications/{created!.Id}"
        );

        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var getAfterDeleteResponse = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-applications/{created.Id}"
        );

        await Assert.That(getAfterDeleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateApplication_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            CreateRequest(
                CreateClientId("anonymous"),
                "Anonymous OAuth test application",
                "Native",
                ["https://oidc-playground.akamai.com/redirect_uri"]
            )
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private static OAuthApplicationMutationRequest CreateRequest(
        string clientId,
        string displayName,
        string platform,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string>? postLogoutRedirectUris = null
    ) =>
        new()
        {
            ClientId = clientId,
            DisplayName = displayName,
            Platform = platform,
            RedirectUris = redirectUris,
            PostLogoutRedirectUris = postLogoutRedirectUris ?? [],
        };

    private static string CreateClientId(string prefix) =>
        $"test-{prefix}-{Guid.NewGuid():N}";

    private sealed class OAuthApplicationMutationRequest
    {
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
        public required string Platform { get; init; }
        public required IReadOnlyList<string> RedirectUris { get; init; }
        public IReadOnlyList<string> PostLogoutRedirectUris { get; init; } = [];
    }

    private sealed class OAuthApplicationResponse
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public string? ClientSecret { get; init; }
        public required string DisplayName { get; init; }
        public required string Platform { get; init; }
        public required IReadOnlyList<string> RedirectUris { get; init; }
        public required IReadOnlyList<string> PostLogoutRedirectUris { get; init; }
    }

    private sealed class OAuthApplicationListResponse
    {
        public required IReadOnlyList<OAuthApplicationResponse> Items { get; init; }
    }
}
