using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthDirectory;

public class OAuthDirectoryTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task List_IncludesCreatedApplicationWithOwner()
    {
        var created = await CreateApplicationAsync("list");

        var response = await AuthenticatedClient.HttpClient.GetAsync("/admin/oauth-directory/applications");
        var result = await response.Content.ReadFromJsonAsync<DirectoryListResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();

        var item = result!.Items.SingleOrDefault(i => i.Id == created.Id);
        await Assert.That(item).IsNotNull();
        await Assert.That(item!.ClientId).IsEqualTo(created.ClientId);
        await Assert.That(item.OwnerUserId).IsNotNull();
        await Assert.That(item.TotalAuthorizations).IsEqualTo(0);

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created.Id}");
    }

    [Test]
    public async Task Get_ReturnsDetailsAndPermissions()
    {
        var created = await CreateApplicationAsync("get");

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-directory/applications/{created.Id}"
        );
        var result = await response.Content.ReadFromJsonAsync<DirectoryGetResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Application.Id).IsEqualTo(created.Id);
        await Assert.That(result.Application.ClientType).IsEqualTo("public");
        await Assert.That(result.Permissions).Contains("gt:authorization_code");
        await Assert.That(result.Requirements).Contains("ft:pkce");

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created.Id}");
    }

    [Test]
    public async Task History_ReturnsOkWithItems()
    {
        var created = await CreateApplicationAsync("history");

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-directory/applications/{created.Id}/history"
        );
        var result = await response.Content.ReadFromJsonAsync<DirectoryHistoryResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TotalCount).IsEqualTo(0);
        await Assert.That(result.Items).IsEmpty();

        await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{created.Id}");
    }

    [Test]
    [Arguments("")]
    [Arguments("/history")]
    public async Task NonExistentApplication_ReturnsNotFound(string suffix)
    {
        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/admin/oauth-directory/applications/{Guid.NewGuid()}{suffix}"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task List_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.HttpClient.GetAsync("/admin/oauth-directory/applications");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private async Task<CreatedApplication> CreateApplicationAsync(string suffix)
    {
        var response = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            new
            {
                ClientId = $"test-directory-{suffix}-{Guid.NewGuid():N}",
                DisplayName = $"Directory {suffix} test application",
                Platform = "Native",
                RedirectUris = new[] { "https://oidc-playground.akamai.com/redirect_uri" },
                PostLogoutRedirectUris = Array.Empty<string>(),
            }
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreatedApplication>();
        await Assert.That(created).IsNotNull();
        return created!;
    }

    private sealed class CreatedApplication
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
    }

    private sealed class DirectoryApplication
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public string? ClientType { get; init; }
        public Guid? OwnerUserId { get; init; }
        public int TotalAuthorizations { get; init; }
    }

    private sealed class DirectoryListResponse
    {
        public required IReadOnlyList<DirectoryApplication> Items { get; init; }
    }

    private sealed class DirectoryGetResponse
    {
        public required DirectoryApplication Application { get; init; }
        public required IReadOnlyList<string> Permissions { get; init; }
        public required IReadOnlyList<string> Requirements { get; init; }
    }

    private sealed class DirectoryHistoryResponse
    {
        public int TotalCount { get; init; }
        public required IReadOnlyList<object> Items { get; init; }
    }
}
