using HackOMania.Tests.Data;
using HackOMania.Tests.Helpers;
using HackOMania.Tests.Models;
using System.Net.Http.Json;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class ResourcesTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Participant Resources Test {suffix}",
            Description = "A test hackathon for participant resources tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PRES{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<(Guid HackathonId, Guid ResourceId)> CreateHackathonWithResourceAsync(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Create hackathon
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create a resource
        var resourceRequest = new
        {
            Name = "Test Resource",
            Description = "A test resource",
            RedemptionStmt = "return true;",
            IsPublished = true,
        };
        var resourceResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/resources",
            resourceRequest
        );
        var resource = await resourceResponse.Content.ReadFromJsonAsync<ResourceResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        return (hackathon.Id, resource!.Id);
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient httpClient)
    {
        var whoami = await httpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
        return whoami!.Id;
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListResources_AsParticipant_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var (hackathonId, _) = await CreateHackathonWithResourceAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/resources"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantResourcesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListResources_HidesUnpublishedResources(AuthenticatedHttpClientDataClass client)
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);

        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        await TestDataHelper.CreateResourceAsync(client.HttpClient, hackathon.Id, isPublished: true);
        await TestDataHelper.CreateResourceAsync(client.HttpClient, hackathon.Id, isPublished: false);

        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/resources"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantResourcesListResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).Count().IsEqualTo(1);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListResources_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create and join a hackathon to be a participant
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Act - Try to get resources for an invalid hackathon
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/resources"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RedeemResource_WithValidResource_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, resourceId) = await CreateHackathonWithResourceAsync(client);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/resources/{resourceId}/redemptions",
            null
        );
        var result = await response.Content.ReadFromJsonAsync<ResourceRedemptionResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ResourceId).IsEqualTo(resourceId);
        await Assert.That(result.HackathonId).IsEqualTo(hackathonId);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RedeemResource_WithInvalidResourceId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{Guid.NewGuid()}/redemptions",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RedeemResource_WithUnpublishedResource_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        var resource = await TestDataHelper.CreateResourceAsync(
            client.HttpClient,
            hackathon.Id,
            isPublished: false
        );
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        var response = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListResources_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/resources"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task RedeemResource_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants/{Guid.NewGuid()}/resources/{Guid.NewGuid()}/redemptions",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
