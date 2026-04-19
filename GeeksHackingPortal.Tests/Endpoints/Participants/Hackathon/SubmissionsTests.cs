using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;
using System.Net.Http.Json;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;

public class SubmissionsTests
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
            Name = $"Participant Submissions Test {suffix}",
            Description = "A test hackathon for participant submissions tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PSBM{suffix}",
            EventStartDate = now.AddDays(-1),
            EventEndDate = now.AddDays(2),
            SubmissionsStartDate = now.AddHours(-1),
            SubmissionsEndDate = now.AddDays(1),
            JudgingStartDate = now.AddDays(1).AddHours(1),
            JudgingEndDate = now.AddDays(1).AddHours(12),
            IsPublished = true,
        };
    }

    private static object CreateValidSubmissionRequest(
        Guid challengeId,
        string title,
        string summary
    )
    {
        return new
        {
            ChallengeId = challengeId,
            Title = title,
            Summary = summary,
            RepoUri = new Uri("https://github.com/test/repo"),
            DemoUri = new Uri("https://demo.example.com"),
            SlidesUri = new Uri("https://slides.example.com"),
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
            Sponsor = "Test Sponsor",
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
    public async Task CreateSubmission_AsTeamMember_ReturnsOk()
    {
        // Arrange
        var (hackathonId, teamId, challengeId) = await CreateHackathonWithTeamAndChallengeAsync(
            client
        );
        var title = "Test Submission";
        var summary = "A test submission summary";
        var request = CreateValidSubmissionRequest(
            challengeId,
            title,
            summary
        );

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<CreateSubmissionResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(title);
        await Assert.That(result.ChallengeId).IsEqualTo(challengeId);
    }

    [Test]
    public async Task ListSubmissions_AsTeamMember_ReturnsOk()
    {
        // Arrange
        var (hackathonId, teamId, challengeId) = await CreateHackathonWithTeamAndChallengeAsync(
            client
        );

        // Create a submission first
        var createRequest = CreateValidSubmissionRequest(
            challengeId,
            "List Test Submission",
            "A submission for list test"
        );
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
    public async Task CreateSubmission_WithInvalidChallengeId_ReturnsError()
    {
        // Arrange
        var (hackathonId, teamId, _) = await CreateHackathonWithTeamAndChallengeAsync(client);
        var request = CreateValidSubmissionRequest(
            Guid.NewGuid(),
            "Invalid Challenge Submission",
            "Should fail"
        );

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateSubmission_BeforeSubmissionStartDate_ReturnsBadRequest()
    {
        // Arrange - Create a hackathon where submissions are not open yet
        var now = DateTimeOffset.UtcNow;
        var hackathonRequest = new CreateHackathonRequest
        {
            Name = $"Submission Window Test {Guid.NewGuid().ToString()[..8]}",
            Description = "A test hackathon with upcoming submission window",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = "SBFR" + Guid.NewGuid().ToString()[..8],
            EventStartDate = now.AddDays(1),
            EventEndDate = now.AddDays(5),
            SubmissionsStartDate = now.AddDays(2),
            SubmissionsEndDate = now.AddDays(3),
            JudgingStartDate = now.AddDays(3).AddHours(1),
            JudgingEndDate = now.AddDays(4),
            IsPublished = true,
        };

        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        await Assert.That(hackathonResponse.IsSuccessStatusCode).IsTrue();
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var challengeRequest = new
        {
            Title = "Submission Window Challenge",
            Description = "A challenge for submission window tests",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "true",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        await Assert.That(challengeResponse.IsSuccessStatusCode).IsTrue();
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            new { Name = "Submission Window Team", Description = "Team for submission timing" }
        );
        await Assert.That(teamResponse.IsSuccessStatusCode).IsTrue();
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        var request = CreateValidSubmissionRequest(
            challenge!.Id,
            "Too Early Submission",
            "Should fail before submissions start"
        );

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team!.Id}/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateSubmission_WithInvalidTeamId_ReturnsNotFound()
    {
        // Arrange
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        await Assert.That(hackathonResponse.IsSuccessStatusCode).IsTrue();
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            new
            {
                Title = "Invalid Team Challenge",
                Description = "A challenge for invalid team submission tests",
                Sponsor = "Test Sponsor",
                SelectionCriteriaStmt = "true",
                IsPublished = true,
            }
        );
        await Assert.That(challengeResponse.IsSuccessStatusCode).IsTrue();
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        var request = CreateValidSubmissionRequest(
            challenge!.Id,
            "Invalid Team Submission",
            "Should fail"
        );

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
    public async Task CreateSubmission_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = CreateValidSubmissionRequest(
            Guid.NewGuid(),
            "Unauthorized Submission",
            "Should fail"
        );

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams/{Guid.NewGuid()}/submissions",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ListSubmissions_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams/{Guid.NewGuid()}/submissions"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

