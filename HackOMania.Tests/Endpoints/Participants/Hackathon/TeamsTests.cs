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

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinTeamByCode_WithValidCode_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon and team with the first user
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var createRequest = new { Name = "Join Code Team", Description = "Team to join by code" };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest
        );
        var createdTeam = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Create a second user to join the team
        var secondUser = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 999,
            GitHubLogin = "second-user",
            Name = "Second User",
            Email = "second@example.com",
        };
        await secondUser.InitializeAsync();

        // Second user joins the hackathon as participant
        await secondUser.HttpClient.PostAsync($"/participants/hackathons/{hackathonId}/join", null);

        // Act - Second user joins the team using the code
        var response = await secondUser.HttpClient.PostAsJsonAsync(
            "/participants/teams/join",
            new { createdTeam!.JoinCode }
        );
        var result = await response.Content.ReadFromJsonAsync<JoinTeamByCodeResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TeamId).IsEqualTo(createdTeam.Id);
        await Assert.That(result.HackathonId).IsEqualTo(hackathonId);
        await Assert.That(result.AutoJoinedHackathon).IsFalse();

        // Cleanup
        await secondUser.DisposeAsync();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinTeamByCode_WithoutBeingParticipant_AutoJoinsHackathon(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - First user creates hackathon and team
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join as participant and create team
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);
        var createRequest = new
        {
            Name = "Auto Join Team",
            Description = "Team for auto-join test",
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            createRequest
        );
        var createdTeam = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Create a second user who is NOT a participant
        var secondUser = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 998,
            GitHubLogin = "third-user",
            Name = "Third User",
            Email = "third@example.com",
        };
        await secondUser.InitializeAsync();

        // Act - Second user joins using the code (NOT a participant yet)
        var response = await secondUser.HttpClient.PostAsJsonAsync(
            "/participants/teams/join",
            new { createdTeam!.JoinCode }
        );
        var result = await response.Content.ReadFromJsonAsync<JoinTeamByCodeResponse>();

        // Assert - Should work and auto-join the hackathon
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.AutoJoinedHackathon).IsTrue();
        await Assert.That(result.TeamId).IsEqualTo(createdTeam.Id);
        await Assert.That(result.HackathonId).IsEqualTo(hackathon.Id);

        // Cleanup
        await secondUser.DisposeAsync();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinTeamByCode_WithInvalidCode_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/teams/join",
            new { JoinCode = "INVALID_JOIN_CODE_DOES_NOT_EXIST" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinTeamByCode_WhenAlreadyInTeam_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon and two teams
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Create first team (user joins it automatically)
        var createRequest1 = new { Name = "First Team", Description = "User's current team" };
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest1
        );

        // Leave first team and create second team
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/leave",
            new { }
        );
        var createRequest2 = new { Name = "Second Team", Description = "Team to try to join" };
        var createResponse2 = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest2
        );
        var secondTeam = await createResponse2.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Leave and rejoin first team
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/leave",
            new { }
        );
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            createRequest1
        );

        // Act - Try to join second team while already in first team
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/teams/join",
            new { secondTeam!.JoinCode }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task JoinTeamByCode_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/teams/join",
            new { JoinCode = "ANYCODE" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
