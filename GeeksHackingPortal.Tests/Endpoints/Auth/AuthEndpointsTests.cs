using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Auth;

public class AuthEndpointsTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass authenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass client { get; init; }
    private const string TestGitHubLogin = "qin-guan";
    private const long TestGitHubId = 1;
    private const string TestFirstName = "Qin";
    private const string TestLastName = "Guan";
    private const string TestName = "Qin Guan";
    private const string TestEmail = "qin-guan@outlook.com";

    [Test]
    public async Task WhoAmI_WithAuthentication_ReturnsCurrentUser()
    {
        // Act
        var response = await authenticatedClient.HttpClient.GetAsync("/auth/whoami");
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
    public async Task WhoAmI_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await client.HttpClient.GetAsync("/auth/whoami");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Impersonate_InDevelopment_CreatesUserAndReturnsOk()
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
    public async Task Impersonate_WithExistingUser_ReturnsOk()
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

    [Test]
    public async Task Impersonate_WithConcurrentRequests_ReturnsOk()
    {
        // Arrange - use a unique identity so this test always covers first-time account creation.
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var request = new
        {
            GitHubId = timestamp,
            GitHubLogin = $"concurrent-user-{timestamp}",
            FirstName = "Concurrent",
            LastName = "User",
            Email = $"concurrent-user-{timestamp}@example.com",
        };

        // Act
        var responses = await Task.WhenAll(
            Enumerable
                .Range(0, 8)
                .Select(_ => client.HttpClient.PostAsJsonAsync("/auth/impersonate", request))
        );

        // Assert
        await Assert
            .That(responses.All(response => response.StatusCode == HttpStatusCode.OK))
            .IsTrue();
    }
}


