using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class JudgesTests
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
            Name = $"Judges Test Hackathon {suffix}",
            Description = "A test hackathon for judges tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"JDGE{suffix}",
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
    public async Task CreateJudge_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var request = new { Name = "Test Judge" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/judges",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<JudgeResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(request.Name);
        await Assert.That(result.Secret).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.Active).IsTrue();
    }

    [Test]
    public async Task ListJudges_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Create a judge
        var createRequest = new { Name = "List Test Judge" };
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/judges",
            createRequest
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/judges"
        );
        var result = await response.Content.ReadFromJsonAsync<JudgesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Judges).IsNotNull();
    }

    [Test]
    public async Task CreateJudge_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Arrange
        var request = new { Name = "Invalid Hackathon Judge" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/judges",
            request
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateJudge_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new { Name = "Unauthorized Judge" };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/judges",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

