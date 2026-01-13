using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class SubmissionsTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Participant Submissions Test {suffix}",
            Description = "A test hackathon for participant submissions tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PSBM{suffix}",
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
        Guid TeamId,
        Guid ChallengeId
    )> CreateHackathonWithTeamAndChallengeAsync(AuthenticatedHttpClientDataClass client)
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
            Title = "Submission Test Challenge",
            Description = "A challenge for submission tests",
            SelectionCriteriaStmt = "true",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        // Create a team
        var teamRequest = new
        {
            Name = "Submission Test Team",
            Description = "A team for submission tests",
        };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        return (hackathon.Id, team!.Id, challenge!.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateSubmission_AsTeamMember_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, teamId, challengeId) = await CreateHackathonWithTeamAndChallengeAsync(
            client
        );
        var request = new
        {
            ChallengeId = challengeId,
            Title = "Test Submission",
            Summary = "A test submission summary",
            RepoUri = new Uri("https://github.com/test/repo"),
            DemoUri = new Uri("https://demo.example.com"),
            SlidesUri = new Uri("https://slides.example.com"),
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions",
            request
        );
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<CreateSubmissionResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(request.Title);
        await Assert.That(result.ChallengeId).IsEqualTo(challengeId);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListSubmissions_AsTeamMember_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, teamId, challengeId) = await CreateHackathonWithTeamAndChallengeAsync(
            client
        );

        // Create a submission first
        var createRequest = new
        {
            ChallengeId = challengeId,
            Title = "List Test Submission",
            Summary = "A submission for list test",
            RepoUri = new Uri("https://github.com/test/list-repo"),
        };
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions",
            createRequest
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions"
        );
        var result = await response.Content.ReadFromJsonAsync<TeamSubmissionsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Submissions).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateSubmission_WithInvalidChallengeId_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, teamId, _) = await CreateHackathonWithTeamAndChallengeAsync(client);
        var request = new
        {
            ChallengeId = Guid.NewGuid(),
            Title = "Invalid Challenge Submission",
            Summary = "Should fail",
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateSubmission_WithInvalidTeamId_ReturnsNotFound(
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

        var request = new
        {
            ChallengeId = Guid.NewGuid(),
            Title = "Invalid Team Submission",
            Summary = "Should fail",
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{Guid.NewGuid()}/submissions",
            request
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task CreateSubmission_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Arrange
        var request = new
        {
            ChallengeId = Guid.NewGuid(),
            Title = "Unauthorized Submission",
            Summary = "Should fail",
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams/{Guid.NewGuid()}/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListSubmissions_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams/{Guid.NewGuid()}/submissions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
