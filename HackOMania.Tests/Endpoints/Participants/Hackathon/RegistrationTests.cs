using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class RegistrationTests
{
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListRegistrationQuestions_AsParticipant_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetRegistrationSubmissions_AsParticipant_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SubmitRegistration_WithEmptySubmissions_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListRegistrationQuestions_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListRegistrationQuestions_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/registration/questions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task SubmitRegistration_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Arrange
        var request = new { Submissions = Array.Empty<object>() };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/registration/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
