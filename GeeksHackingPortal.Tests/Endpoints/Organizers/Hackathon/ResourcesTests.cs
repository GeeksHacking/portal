using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

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
            Name = $"Resources Test Hackathon {suffix}",
            Description = "A test hackathon for resources tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"RSRC{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> CreateHackathonAsync(AuthenticatedHttpClientDataClass client)
    {
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();
        return result!.Id;
    }

    [Test]
    public async Task CreateResource_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(Client);
        var request = new
        {
            Name = "Test Resource",
            Description = "A test resource description",
            RedemptionStmt = "return true;",
            IsPublished = true,
        };

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/resources",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<ResourceResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(request.Name);
        await Assert.That(result.Description).IsEqualTo(request.Description);
        await Assert.That(result.IsPublished).IsTrue();
    }

    [Test]
    public async Task ListResources_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(Client);

        // Create a resource
        var createRequest = new
        {
            Name = "List Test Resource",
            Description = "A resource for list test",
            RedemptionStmt = "return true;",
            IsPublished = false,
        };
        await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/resources",
            createRequest
        );

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/resources"
        );
        var result = await response.Content.ReadFromJsonAsync<ResourcesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Resources).IsNotNull();
        await Assert.That(result.Resources!.Single().IsPublished).IsFalse();
    }

    [Test]
    public async Task CreateResource_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Arrange
        var request = new
        {
            Name = "Invalid Hackathon Resource",
            Description = "Should fail",
            RedemptionStmt = "return true;",
            IsPublished = true,
        };

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/resources",
            request
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateResource_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Name = "Unauthorized Resource",
            Description = "Should fail",
            RedemptionStmt = "return true;",
            IsPublished = true,
        };

        // Act
        var response = await AnonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/resources",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

