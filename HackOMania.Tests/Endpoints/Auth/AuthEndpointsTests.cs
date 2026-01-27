using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Auth;

public class AuthEndpointsTests
{
    private const string TestGitHubLogin = "qin-guan";
    private const long TestGitHubId = 1;
    private const string TestFirstName = "Qin";
    private const string TestLastName = "Guan";
    private const string TestName = "Qin Guan";
    private const string TestEmail = "qin-guan@outlook.com";

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WhoAmI_WithAuthentication_ReturnsCurrentUser(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync("/auth/whoami");
        var result = await response.Content.ReadFromJsonAsync<WhoAmIResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.GitHubLogin).IsEqualTo(TestGitHubLogin);
        await Assert.That(result.Email).IsEqualTo(TestEmail);
        await Assert.That(result.Name).IsEqualTo(TestName);
        await Assert.That(result.FirstName).IsEqualTo(TestFirstName);
        await Assert.That(result.LastName).IsEqualTo(TestLastName);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task WhoAmI_WithoutAuthentication_ReturnsUnauthorized(HttpClientDataClass client)
    {
        // Act
        var response = await client.HttpClient.GetAsync("/auth/whoami");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task Impersonate_InDevelopment_CreatesUserAndReturnsOk(HttpClientDataClass client)
    {
        // Arrange - Use qin-guan credentials for impersonation
        var request = new
        {
            GitHubId = TestGitHubId,
            GitHubLogin = TestGitHubLogin,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Email = TestEmail,
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync("/auth/impersonate", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task Impersonate_WithExistingUser_ReturnsOk(HttpClientDataClass client)
    {
        // Arrange - Use qin-guan credentials
        var request = new
        {
            GitHubId = TestGitHubId,
            GitHubLogin = TestGitHubLogin,
            FirstName = TestFirstName,
            LastName = TestLastName,
            Email = TestEmail,
        };

        // Create user first
        await client.HttpClient.PostAsJsonAsync("/auth/impersonate", request);

        // Act - Impersonate the same user again
        var response = await client.HttpClient.PostAsJsonAsync("/auth/impersonate", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
