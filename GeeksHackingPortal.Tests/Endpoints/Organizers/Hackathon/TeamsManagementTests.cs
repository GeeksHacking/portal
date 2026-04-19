using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class TeamsManagementTests
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
            Name = $"Teams Mgmt Test Hackathon {suffix}",
            Description = "A test hackathon for teams management tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"TMMG{suffix}",
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
    public async Task ListTeams_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/teams"
        );
        var result = await response.Content.ReadFromJsonAsync<TeamsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Teams).IsNotNull();
    }

    [Test]
    public async Task ListTeams_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/teams"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListTeams_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/teams"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

