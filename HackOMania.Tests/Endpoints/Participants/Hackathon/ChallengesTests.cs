using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class ChallengesTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Participant Challenges Test {suffix}",
            Description = "A test hackathon for participant challenges tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PCHL{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<(
        Guid HackathonId,
        Guid ChallengeId
    )> CreateHackathonWithPublishedChallengeAsync(AuthenticatedHttpClientDataClass client)
    {
        // Create hackathon
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create a published challenge
        var challengeRequest = new
        {
            Title = "Published Challenge",
            Description = "A published challenge for tests",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        return (hackathon.Id, challenge!.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListChallenges_AsParticipant_ReturnsPublishedChallenges(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, _) = await CreateHackathonWithPublishedChallengeAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/challenges"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantChallengesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Challenges).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetChallenge_PublishedChallenge_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, challengeId) = await CreateHackathonWithPublishedChallengeAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/challenges/{challengeId}"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantChallengeResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(challengeId);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetChallenge_UnpublishedChallenge_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create hackathon and join
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create an unpublished challenge
        var challengeRequest = new
        {
            Title = "Unpublished Challenge",
            Description = "An unpublished challenge",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = false,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/challenges/{challenge!.Id}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetChallenge_WithInvalidId_ReturnsNotFound(
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

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.Id}/challenges/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListChallenges_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/challenges"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
