using System.Net.Http.Json;
using System.Text.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthDirectory;

/// <summary>
/// Verifies that the OAuth directory is strictly root-only and strictly read-only.
/// </summary>
public class OAuthDirectoryAuthorizationTests
{
    private const string DirectoryBase = "/admin/oauth-directory/applications";

    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass RootClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

    [Test]
    public async Task RootClient_IsActuallyRoot()
    {
        var whoAmI = await RootClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");

        await Assert.That(whoAmI).IsNotNull();
        await Assert.That(whoAmI!.IsRoot).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("/{id}")]
    [Arguments("/{id}/history")]
    public async Task Anonymous_ReturnsUnauthorized(string routeTemplate)
    {
        var created = await CreateApplicationAsync(RootClient, "anon");
        try
        {
            var response = await AnonymousClient.HttpClient.GetAsync(BuildRoute(routeTemplate, created.Id));

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await DeleteApplicationAsync(RootClient, created.Id);
        }
    }

    [Test]
    [Arguments("")]
    [Arguments("/{id}")]
    [Arguments("/{id}/history")]
    public async Task NonRootUser_ReturnsForbidden(string routeTemplate)
    {
        var created = await CreateApplicationAsync(RootClient, "nonroot");
        try
        {
            await using var nonRootClient = await CreateNonRootClientAsync();
            var whoAmI = await nonRootClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
            await Assert.That(whoAmI).IsNotNull();
            await Assert.That(whoAmI!.IsRoot).IsFalse();

            var response = await nonRootClient.HttpClient.GetAsync(BuildRoute(routeTemplate, created.Id));

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        }
        finally
        {
            await DeleteApplicationAsync(RootClient, created.Id);
        }
    }

    [Test]
    public async Task NonRootUser_CannotUseOwnerScopedAdminEndpointsEither()
    {
        await using var nonRootClient = await CreateNonRootClientAsync();

        var listResponse = await nonRootClient.HttpClient.GetAsync("/admin/oauth-applications");
        var createResponse = await nonRootClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            BuildCreateRequest("nonroot-create", "Native")
        );

        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Root_DirectoryIsReadOnly_MutatingVerbsAreRejectedAndStateIsUnchanged()
    {
        var created = await CreateApplicationAsync(RootClient, "readonly");
        try
        {
            var mutationPayload = BuildCreateRequest("readonly-mutated", "Native");
            var attempts = new (string Verb, HttpRequestMessage Request)[]
            {
                ("POST list", new HttpRequestMessage(HttpMethod.Post, DirectoryBase)
                {
                    Content = JsonContent.Create(mutationPayload),
                }),
                ("PUT item", new HttpRequestMessage(HttpMethod.Put, $"{DirectoryBase}/{created.Id}")
                {
                    Content = JsonContent.Create(mutationPayload),
                }),
                ("PATCH item", new HttpRequestMessage(HttpMethod.Patch, $"{DirectoryBase}/{created.Id}")
                {
                    Content = JsonContent.Create(mutationPayload),
                }),
                ("DELETE item", new HttpRequestMessage(HttpMethod.Delete, $"{DirectoryBase}/{created.Id}")),
                ("DELETE history", new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"{DirectoryBase}/{created.Id}/history"
                )),
                ("POST history", new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{DirectoryBase}/{created.Id}/history"
                )
                {
                    Content = JsonContent.Create(new { }),
                }),
            };

            foreach (var (_, request) in attempts)
            {
                using var response = await RootClient.HttpClient.SendAsync(request);

                // No route is registered for these verbs, so the framework must reject them
                // before any handler runs: either the route does not exist or the verb is not allowed.
                await Assert
                    .That(response.StatusCode)
                    .IsEqualTo(HttpStatusCode.NotFound)
                    .Or.IsEqualTo(HttpStatusCode.MethodNotAllowed);
            }

            // The application must still exist, untouched, after every attempt above.
            var afterResponse = await RootClient.HttpClient.GetAsync($"{DirectoryBase}/{created.Id}");
            var after = await afterResponse.Content.ReadFromJsonAsync<DirectoryGetResponse>();

            await Assert.That(afterResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(after).IsNotNull();
            await Assert.That(after!.Application.ClientId).IsEqualTo(created.ClientId);
            await Assert.That(after.Application.DisplayName).IsEqualTo(created.DisplayName);
        }
        finally
        {
            await DeleteApplicationAsync(RootClient, created.Id);
        }
    }

    [Test]
    public async Task Root_SeesApplicationsOwnedByOtherAdmins_ButOwnerScopedListDoesNot()
    {
        await using var otherRootClient = await CreateOtherRootClientAsync();
        var otherRoot = await otherRootClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
        await Assert.That(otherRoot).IsNotNull();
        await Assert.That(otherRoot!.IsRoot).IsTrue();

        var created = await CreateApplicationAsync(otherRootClient, "other-owner");
        try
        {
            // The owner-scoped endpoints hide it from the first root user...
            var ownerScopedList = await RootClient.HttpClient.GetFromJsonAsync<DirectoryListResponse>(
                "/admin/oauth-applications"
            );
            var ownerScopedGet = await RootClient.HttpClient.GetAsync($"/admin/oauth-applications/{created.Id}");

            await Assert.That(ownerScopedList!.Items.Any(i => i.Id == created.Id)).IsFalse();
            await Assert.That(ownerScopedGet.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

            // ...while the root directory shows it with the correct owner.
            var directoryList = await RootClient.HttpClient.GetFromJsonAsync<DirectoryListResponse>(DirectoryBase);
            var directoryItem = directoryList!.Items.SingleOrDefault(i => i.Id == created.Id);
            var directoryGetResponse = await RootClient.HttpClient.GetAsync($"{DirectoryBase}/{created.Id}");
            var directoryGet = await directoryGetResponse.Content.ReadFromJsonAsync<DirectoryGetResponse>();
            var historyResponse = await RootClient.HttpClient.GetAsync($"{DirectoryBase}/{created.Id}/history");

            await Assert.That(directoryItem).IsNotNull();
            await Assert.That(directoryItem!.OwnerUserId).IsEqualTo(otherRoot.Id);
            await Assert.That(directoryItem.OwnerEmail).IsEqualTo(otherRoot.Email);
            await Assert.That(directoryGetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(directoryGet!.Application.OwnerUserId).IsEqualTo(otherRoot.Id);
            await Assert.That(historyResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
        finally
        {
            await DeleteApplicationAsync(otherRootClient, created.Id);
        }
    }

    [Test]
    public async Task Directory_NeverExposesClientSecret()
    {
        var createResponse = await RootClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            BuildCreateRequest("secret", "Web")
        );
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedApplication>();
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(created!.ClientSecret).IsNotNull();

        try
        {
            var listJson = await RootClient.HttpClient.GetStringAsync(DirectoryBase);
            var getJson = await RootClient.HttpClient.GetStringAsync($"{DirectoryBase}/{created.Id}");

            foreach (var json in new[] { listJson, getJson })
            {
                await Assert.That(json.Contains(created.ClientSecret!, StringComparison.Ordinal)).IsFalse();
                await Assert.That(ContainsPropertyIgnoringCase(json, "clientSecret")).IsFalse();
            }
        }
        finally
        {
            await DeleteApplicationAsync(RootClient, created.Id);
        }
    }

    private static string BuildRoute(string routeTemplate, string id) =>
        DirectoryBase + routeTemplate.Replace("{id}", id);

    private static object BuildCreateRequest(string suffix, string platform) =>
        new
        {
            ClientId = $"test-directory-auth-{suffix}-{Guid.NewGuid():N}",
            DisplayName = $"Directory auth {suffix} test application",
            Platform = platform,
            RedirectUris = new[] { "https://oidc-playground.akamai.com/redirect_uri" },
            PostLogoutRedirectUris = Array.Empty<string>(),
        };

    private static async Task<CreatedApplication> CreateApplicationAsync(
        AuthenticatedHttpClientDataClass client,
        string suffix
    )
    {
        var response = await client.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            BuildCreateRequest(suffix, "Native")
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreatedApplication>();
        await Assert.That(created).IsNotNull();
        return created!;
    }

    private static Task DeleteApplicationAsync(AuthenticatedHttpClientDataClass client, string id) =>
        client.HttpClient.DeleteAsync($"/admin/oauth-applications/{id}");

    private static async Task<AuthenticatedHttpClientDataClass> CreateNonRootClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var gitHubId = Math.Abs(BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0));
        if (gitHubId == 0)
        {
            gitHubId = 1;
        }

        var client = new AuthenticatedHttpClientDataClass
        {
            GitHubId = gitHubId,
            GitHubLogin = $"nonroot-{suffix}",
            FirstName = "NonRoot",
            LastName = suffix,
            Email = $"nonroot-{suffix}@example.com",
        };

        await client.InitializeAsync();
        return client;
    }

    // "gunnicorn" is listed under Admin:AdminGitHubLogins in appsettings.Development.json and is
    // seeded by the DbMigrator with this GitHubId, so impersonation resolves the existing account.
    private static async Task<AuthenticatedHttpClientDataClass> CreateOtherRootClientAsync()
    {
        var client = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 47025159,
            GitHubLogin = "gunnicorn",
            FirstName = "gunnicorn",
            LastName = "",
            Email = "anggunq@hotmail.com",
        };

        await client.InitializeAsync();
        return client;
    }

    private static bool ContainsPropertyIgnoringCase(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return ContainsPropertyIgnoringCase(document.RootElement, propertyName);
    }

    private static bool ContainsPropertyIgnoringCase(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (
                        string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                        || ContainsPropertyIgnoringCase(property.Value, propertyName)
                    )
                    {
                        return true;
                    }
                }
                return false;
            case JsonValueKind.Array:
                return element.EnumerateArray().Any(item => ContainsPropertyIgnoringCase(item, propertyName));
            default:
                return false;
        }
    }

    private sealed class CreatedApplication
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
        public string? ClientSecret { get; init; }
    }

    private sealed class DirectoryApplication
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
        public Guid? OwnerUserId { get; init; }
        public string? OwnerEmail { get; init; }
    }

    private sealed class DirectoryListResponse
    {
        public required IReadOnlyList<DirectoryApplication> Items { get; init; }
    }

    private sealed class DirectoryGetResponse
    {
        public required DirectoryApplication Application { get; init; }
    }
}
