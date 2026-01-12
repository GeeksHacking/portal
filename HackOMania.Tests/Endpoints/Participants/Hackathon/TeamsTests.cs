using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class TeamsTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Teams Test Hackathon {suffix}",
            Description = "A test hackathon for teams tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"TEAM{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> CreatePublishedHackathonAndJoinAsync(
        AuthenticatedHttpClientDataClass client
    )
    {
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{result!.Id}/join", null);

        return result.Id;
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateTeam_AsParticipant_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var request = new { Name = "Test Team", Description = "A test team description" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(request.Name);
        await Assert.That(result.JoinCode).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateTeam_WhenAlreadyInTeam_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Create first team
        var createRequest1 = new { Name = "First Team", Description = "First team" };
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest1
        );

        // Act - Try to create another team
        var createRequest2 = new { Name = "Second Team", Description = "Second team" };
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest2
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetMyTeam_AfterCreating_ReturnsTeam(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var createRequest = new { Name = "Get Mine Team", Description = "Team for get mine test" };
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest
        );

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/me"
        );
        var result = await response.Content.ReadFromJsonAsync<MyTeamResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(createRequest.Name);
        await Assert.That(result.Members).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetMyTeam_WithNoTeam_ReturnsNotFound(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Act - Don't create a team, just try to get it
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/me"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task LeaveTeam_WhenInTeam_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var createRequest = new { Name = "Leave Team Test", Description = "Team to leave" };
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest
        );

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/leave",
            new { }
        );
        var result = await response.Content.ReadFromJsonAsync<LeaveTeamResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task LeaveTeam_WhenNotInTeam_ReturnsError(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Act - Try to leave without being in a team
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/leave",
            new { }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateTeam_AsTeamMember_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var createRequest = new { Name = "Update Team Test", Description = "Original description" };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest
        );
        var createdTeam = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        var updateRequest = new { Name = "Updated Team Name", Description = "Updated description" };

        // Act
        var response = await client.HttpClient.PatchAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{createdTeam!.Id}",
            updateRequest
        );
        var result = await response.Content.ReadFromJsonAsync<UpdateTeamResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(updateRequest.Name);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task CreateTeam_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Arrange
        var request = new { Name = "Unauthorized Team", Description = "Should fail" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CreateTeam_WithoutJoiningHackathon_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create hackathon but don't join as participant
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        var teamRequest = new { Name = "No Join Team", Description = "Should fail" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon!.Id}/teams",
            teamRequest
        );

        // Assert - Should return 403 Forbidden or error since not a participant
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.Forbidden)
            .Or.IsEqualTo(HttpStatusCode.BadRequest);
    }
}
