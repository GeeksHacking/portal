using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;

public class ChallengesTests
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
            Sponsor = "Test Sponsor",
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
    public async Task ListChallenges_AsParticipant_ReturnsPublishedChallenges()
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
        await Assert.That(result.Challenges).IsNotEmpty();

        var challenge = result.Challenges!.First();
        await Assert.That(challenge.Title).IsEqualTo("Published Challenge");
        await Assert.That(challenge.Description).IsEqualTo("A published challenge for tests");
        await Assert.That(challenge.SelectionCriteriaStmt).IsEqualTo("Test criteria");
        await Assert.That(challenge.TeamCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetChallenge_PublishedChallenge_ReturnsOk()
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
    public async Task GetChallenge_UnpublishedChallenge_ReturnsNotFound()
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
            Sponsor = "Test Sponsor",
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
    public async Task GetChallenge_WithInvalidId_ReturnsNotFound()
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
    public async Task ListChallenges_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/challenges"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateChallenge_CacheInvalidation_ReturnsUpdatedData()
    {
        // Arrange - Create hackathon with a challenge
        var (hackathonId, challengeId) = await CreateHackathonWithPublishedChallengeAsync(client);

        // Act 1 - Get challenge to populate cache
        var response1 = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/challenges/{challengeId}"
        );
        var result1 = await response1.Content.ReadFromJsonAsync<ParticipantChallengeResponse>();
        await Assert.That(result1).IsNotNull();
        await Assert.That(result1!.Title).IsEqualTo("Published Challenge");

        // Act 2 - Update the challenge
        var updateRequest = new
        {
            Title = "Updated Challenge Title",
            Description = "Updated description",
        };
        var updateResponse = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{challengeId}",
            updateRequest
        );
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Get challenge again to verify cache was invalidated
        var response2 = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/challenges/{challengeId}"
        );
        var result2 = await response2.Content.ReadFromJsonAsync<ParticipantChallengeResponse>();

        // Assert - Should return updated data, not cached old data
        await Assert.That(result2).IsNotNull();
        await Assert.That(result2!.Title).IsEqualTo("Updated Challenge Title");
        await Assert.That(result2.Description).IsEqualTo("Updated description");
    }
}

