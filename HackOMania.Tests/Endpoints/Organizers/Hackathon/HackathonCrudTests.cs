using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Organizers.Hackathon;

public class HackathonCrudTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Test Hackathon {suffix}",
            Description = "A test hackathon for integration tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"TEST{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = false,
        };
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateHackathon_WithValidRequest_ReturnsCreated(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);

        // Act
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(request.Name);
        await Assert.That(result.Description).IsEqualTo(request.Description);
        await Assert.That(result.Venue).IsEqualTo(request.Venue);
        await Assert.That(result.ShortCode).IsEqualTo(request.ShortCode);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task CreateHackathon_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Arrange
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);

        // Act
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListHackathons_WithAuthentication_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon first
        var createRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", createRequest);

        // Act
        var response = await client.HttpClient.GetAsync("/organizers/hackathons");
        var result = await response.Content.ReadFromJsonAsync<OrganizersHackathonsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Hackathons).IsNotNull();
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListHackathons_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync("/organizers/hackathons");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetHackathon_AsOrganizer_ReturnsHackathon(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon
        var createRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            createRequest
        );
        var createdHackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{createdHackathon!.Id}"
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(createdHackathon.Id);
        await Assert.That(result.Name).IsEqualTo(createRequest.Name);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetHackathon_WithInvalidId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync($"/organizers/hackathons/{Guid.NewGuid()}");

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateHackathon_WithValidRequest_ReturnsUpdatedHackathon(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon
        var createRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            createRequest
        );
        var createdHackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var updateRequest = new UpdateHackathonRequest
        {
            Name = "Updated Hackathon Name",
            Description = "Updated description",
        };

        // Act
        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{createdHackathon!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(updateRequest.Name);
        await Assert.That(result.Description).IsEqualTo(updateRequest.Description);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateHackathon_PublishHackathon_ReturnsUpdatedHackathon(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create an unpublished hackathon
        var createRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        createRequest.IsPublished = false;
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            createRequest
        );
        var createdHackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var updateRequest = new UpdateHackathonRequest { IsPublished = true };

        // Act
        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{createdHackathon!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsPublished).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateHackathon_GitHubRepositorySettings_RoundTrips(
        AuthenticatedHttpClientDataClass client
    )
    {
        var createRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            createRequest
        );
        var createdHackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var updateRequest = new UpdateHackathonRequest
        {
            GitHubRepositorySettings = new GitHubRepositorySettingsRequest
            {
                IsRepositoryCheckingEnabled = true,
                IsRepositoryForkingEnabled = true,
                ApiKey = "ghp_test_token_1234567890",
                RepositoryPrefix = "hackomania-",
                OrganizationId = 123456789,
            },
        };

        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{createdHackathon!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.GitHubRepositorySettings.IsRepositoryCheckingEnabled).IsTrue();
        await Assert.That(result.GitHubRepositorySettings.IsRepositoryForkingEnabled).IsTrue();
        await Assert.That(result.GitHubRepositorySettings.HasApiKey).IsTrue();
        await Assert.That(result.GitHubRepositorySettings.RepositoryPrefix).IsEqualTo("hackomania-");
        await Assert.That(result.GitHubRepositorySettings.OrganizationId).IsEqualTo(123456789);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateHackathon_WithInvalidId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var updateRequest = new UpdateHackathonRequest { Name = "Updated Name" };

        // Act
        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}",
            updateRequest
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }
}
