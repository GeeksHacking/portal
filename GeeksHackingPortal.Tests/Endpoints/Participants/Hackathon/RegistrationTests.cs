using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;

public class RegistrationTests
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
            Name = $"Registration Test Hackathon {suffix}",
            Description = "A test hackathon for registration tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"REG{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> CreateHackathonAndJoinAsync(
        AuthenticatedHttpClientDataClass client
    )
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        return hackathon.Id;
    }

    [Test]
    public async Task ListRegistrationQuestions_AsParticipant_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAndJoinAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/registration/questions"
        );
        var result = await response.Content.ReadFromJsonAsync<RegistrationQuestionsResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Categories).IsNotNull();
    }

    [Test]
    public async Task GetRegistrationSubmissions_AsParticipant_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAndJoinAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/registration/submissions"
        );
        var result = await response.Content.ReadFromJsonAsync<RegistrationSubmissionsResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task SubmitRegistration_WithEmptySubmissions_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAndJoinAsync(client);
        var request = new { Submissions = Array.Empty<object>() };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/registration/submissions",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<SubmitRegistrationResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ListRegistrationQuestions_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Arrange - Still need to be a participant somewhere
        await CreateHackathonAndJoinAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/registration/questions"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListRegistrationQuestions_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/registration/questions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SubmitRegistration_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new { Submissions = Array.Empty<object>() };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/registration/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

