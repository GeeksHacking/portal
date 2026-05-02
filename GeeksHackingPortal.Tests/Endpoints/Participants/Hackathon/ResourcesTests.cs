using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;
using System.Net.Http.Json;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;

public class ResourcesTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

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
    public async Task ListResources_AsParticipant_ReturnsOk()
    {
        // Arrange
        var (hackathonId, _) = await CreateHackathonWithResourceAsync(Client);

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/resources"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantResourcesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).IsNotNull();
    }

    [Test]
    public async Task ListResources_HidesUnpublishedResources()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);

        await Client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        await TestDataHelper.CreateResourceAsync(Client.HttpClient, hackathon.Id, isPublished: true);
        await TestDataHelper.CreateResourceAsync(Client.HttpClient, hackathon.Id, isPublished: false);

        var response = await Client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/resources"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantResourcesListResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ListResources_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Arrange - Create and join a hackathon to be a participant
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await Client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Act - Try to get resources for an invalid hackathon
        var response = await Client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/resources"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RedeemResource_WithValidResource_ReturnsOk()
    {
        // Arrange
        var (hackathonId, resourceId) = await CreateHackathonWithResourceAsync(Client);
        var participantUserId = await GetCurrentUserIdAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/resources/{resourceId}/redemptions",
            null
        );
        var result = await response.Content.ReadFromJsonAsync<ResourceRedemptionResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ResourceId).IsEqualTo(resourceId);
        await Assert.That(result.ActivityId).IsEqualTo(hackathonId);
    }

    [Test]
    public async Task RedeemResource_WithInvalidResourceId_ReturnsNotFound()
    {
        // Arrange
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await Client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);
        var participantUserId = await GetCurrentUserIdAsync(Client.HttpClient);

        // Act
        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{Guid.NewGuid()}/redemptions",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RedeemResource_WithUnpublishedResource_ReturnsNotFound()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        var resource = await TestDataHelper.CreateResourceAsync(
            Client.HttpClient,
            hackathon.Id,
            isPublished: false
        );
        var participantUserId = await GetCurrentUserIdAsync(Client.HttpClient);

        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ListResources_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/resources"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RedeemResource_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.PostAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants/{Guid.NewGuid()}/resources/{Guid.NewGuid()}/redemptions",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

