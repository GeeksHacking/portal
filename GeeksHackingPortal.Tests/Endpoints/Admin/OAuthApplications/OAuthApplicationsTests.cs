using System.Net.Http.Json;
using System.Text.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [Test]
    public async Task DeleteOAuthApplication_WithConcurrentRequests_DoesNotReturnServerError()
    {
        var createRequest = new
        {
            ClientId = $"test-client-{Guid.NewGuid():N}",
            DisplayName = "Test OAuth App",
            Platform = 0,
            RedirectUris = new[] { new Uri("https://example.com/callback") },
            PostLogoutRedirectUris = Array.Empty<Uri>(),
        };

        var createResponse = await Client.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            createRequest
        );
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var createdApplication = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var applicationId = createdApplication.GetProperty("id").GetString();
        await Assert.That(applicationId).IsNotNull().And.IsNotEmpty();

        var deletePath = $"/admin/oauth-applications/{applicationId}";
        var deleteResponses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => Client.HttpClient.DeleteAsync(deletePath))
        );

        await Assert.That(deleteResponses.Any(r => r.StatusCode == HttpStatusCode.NoContent)).IsTrue();
        await Assert
            .That(
                deleteResponses.All(r =>
                    r.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound
                )
            )
            .IsTrue();
    }
}
