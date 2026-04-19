using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class OrganizersManagementTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Organizers Test Hackathon {suffix}",
            Description = "A test hackathon for organizers tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"ORGN{suffix}",
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
    public async Task ListOrganizers_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/organizers"
        );
        var result = await response.Content.ReadFromJsonAsync<OrganizersListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Organizers).IsNotNull();
        // Creator should be an organizer
        await Assert.That(result.Organizers!.Count()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ListOrganizers_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListOrganizers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AddOrganizer_WithNonExistentUser_ReturnsError()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var request = new { UserId = Guid.NewGuid(), Type = "Admin" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/organizers",
            request
        );

        // Assert
        // Should return error because user doesn't exist
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AddOrganizer_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new { UserId = Guid.NewGuid(), Type = "Admin" };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

