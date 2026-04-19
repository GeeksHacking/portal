using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

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
            Name = $"Challenge Test Hackathon {suffix}",
            Description = "A test hackathon for challenge tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"CHLNG{suffix}",
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
    public async Task CreateChallenge_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var request = new
        {
            Title = "Test Challenge",
            Description = "A test challenge description",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Innovation and creativity",
            IsPublished = false,
        };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(request.Title);
        await Assert.That(result.Description).IsEqualTo(request.Description);
    }

    [Test]
    public async Task ListChallenges_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Create a challenge
        var createRequest = new
        {
            Title = "List Test Challenge",
            Description = "A challenge for list test",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = true,
        };
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            createRequest
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/challenges"
        );
        var result = await response.Content.ReadFromJsonAsync<ChallengesListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Challenges).IsNotNull();
    }

    [Test]
    public async Task GetChallenge_WithValidId_ReturnsChallenge()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Create a challenge
        var createRequest = new
        {
            Title = "Get Test Challenge",
            Description = "A challenge for get test",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = true,
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            createRequest
        );
        var createdChallenge = await createResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{createdChallenge!.Id}"
        );
        var result = await response.Content.ReadFromJsonAsync<ChallengeDetailResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(createRequest.Title);
    }

    [Test]
    public async Task GetChallenge_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateChallenge_WithValidRequest_ReturnsUpdatedChallenge()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Create a challenge
        var createRequest = new
        {
            Title = "Update Test Challenge",
            Description = "A challenge for update test",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = false,
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            createRequest
        );
        var createdChallenge = await createResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        var updateRequest = new
        {
            Title = "Updated Challenge Title",
            Description = "Updated description",
            IsPublished = true,
        };

        // Act
        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{createdChallenge!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<ChallengeUpdateResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo(updateRequest.Title);
        await Assert.That(result.IsPublished).IsTrue();
    }

    [Test]
    public async Task DeleteChallenge_WithNoSubmissions_ReturnsNoContent()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Create a challenge
        var createRequest = new
        {
            Title = "Delete Test Challenge",
            Description = "A challenge for delete test",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = false,
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            createRequest
        );
        var createdChallenge = await createResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Act
        var response = await client.HttpClient.DeleteAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{createdChallenge!.Id}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteChallenge_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.DeleteAsync(
            $"/organizers/hackathons/{hackathonId}/challenges/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateChallenge_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Title = "Unauthorized Challenge",
            Description = "Should fail",
            Sponsor = "Test Sponsor",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = false,
        };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/challenges",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

