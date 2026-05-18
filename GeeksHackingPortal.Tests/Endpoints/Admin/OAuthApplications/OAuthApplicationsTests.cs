using System.Net.Http.Json;
using System.Text.Json;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Admin.OAuthApplications;

public class OAuthApplicationsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [Test]
    public async Task DeleteOAuthApplication_WhileUpdating_DoesNotReturnServerError()
    {
        const int webPlatform = 0;
        var createRequest = new
        {
            ClientId = $"test-client-{Guid.NewGuid():N}",
            DisplayName = "Test OAuth App",
            Platform = webPlatform,
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
        var updateResponseTask = Client.HttpClient.PutAsJsonAsync(
            deletePath,
            new
            {
                Id = applicationId,
                ClientId = createRequest.ClientId,
                DisplayName = "Updated OAuth App",
                Platform = webPlatform,
                RedirectUris = createRequest.RedirectUris,
                PostLogoutRedirectUris = createRequest.PostLogoutRedirectUris,
                RotateClientSecret = false,
            }
        );
        var deleteResponseTask = Client.HttpClient.DeleteAsync(deletePath);

        await Task.WhenAll(updateResponseTask, deleteResponseTask);

        var updateResponse = await updateResponseTask;
        var deleteResponse = await deleteResponseTask;

        await Assert
            .That(
                updateResponse.StatusCode
                    is HttpStatusCode.OK or HttpStatusCode.NotFound or HttpStatusCode.BadRequest
            )
            .IsTrue();
        await Assert.That(deleteResponse.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound).IsTrue();
    }
}
