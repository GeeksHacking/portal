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

    private static async Task<(
        Guid HackathonId,
        Guid ChallengeId,
        Guid TeamId
    )> CreateHackathonWithChallengeAndTeamAsync(AuthenticatedHttpClientDataClass client)
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
            Title = "Test Challenge",
            Description = "A test challenge for team selection",
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

        // Create a team
        var teamRequest = new { Name = "Test Team", Description = "Team for challenge selection" };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        return (hackathon.Id, challenge!.Id, team!.Id);
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
        await Assert.That(result.Description).IsEqualTo(createRequest.Description);
        await Assert.That(result.ChallengeId).IsNull();
        await Assert.That(result.Members).IsNotNull();
        await Assert.That(result.Members).IsNotEmpty();
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
            FirstName = "Second",
            LastName = "User",
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
            FirstName = "Third",
            LastName = "User",
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

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SelectChallenge_AsTeamMember_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, challengeId, teamId) = await CreateHackathonWithChallengeAndTeamAsync(
            client
        );

        // Act
        var response = await client.HttpClient.PutAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/challenge",
            new { ChallengeId = challengeId }
        );
        var result = await response.Content.ReadFromJsonAsync<SelectChallengeResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TeamId).IsEqualTo(teamId);
        await Assert.That(result.ChallengeId).IsEqualTo(challengeId);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SelectChallenge_WithInvalidChallenge_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var (hackathonId, _, teamId) = await CreateHackathonWithChallengeAndTeamAsync(client);
        var invalidChallengeId = Guid.NewGuid();

        // Act
        var response = await client.HttpClient.PutAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{teamId}/challenge",
            new { ChallengeId = invalidChallengeId }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SelectChallenge_WhenDeadlinePassed_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a hackathon with past submission deadline
        var now = DateTimeOffset.UtcNow;
        var hackathonRequest = new CreateHackathonRequest
        {
            Name = "Past Deadline Hackathon",
            Description = "Hackathon with past submission deadline",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = "PAST" + Guid.NewGuid().ToString()[..8],
            EventStartDate = now.AddDays(-7),
            EventEndDate = now.AddDays(-5),
            SubmissionsStartDate = now.AddDays(-7).AddHours(2),
            SubmissionsEndDate = now.AddDays(-6).AddHours(20), // Past deadline
            JudgingStartDate = now.AddDays(-6).AddHours(21),
            JudgingEndDate = now.AddDays(-5).AddHours(-2),
            IsPublished = true,
        };
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create a published challenge
        var challengeRequest = new
        {
            Title = "Past Deadline Challenge",
            Description = "Challenge for past deadline test",
            SelectionCriteriaStmt = "Test criteria",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Join as participant and create team
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        var teamRequest = new
        {
            Name = "Past Deadline Team",
            Description = "Team for deadline test",
        };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Act
        var response = await client.HttpClient.PutAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team!.Id}/challenge",
            new { ChallengeId = challenge!.Id }
        );

        // Assert - Should return 400 Bad Request with error about deadline
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task SelectChallenge_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PutAsJsonAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/teams/{Guid.NewGuid()}/challenge",
            new { ChallengeId = Guid.NewGuid() }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ChallengeSelection_Workflow_UpdatesTeamCountsAndChallengeIds(
        AuthenticatedHttpClientDataClass client1
    )
    {
        // Arrange - Create hackathon with two challenges
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client1.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create two published challenges
        var challengeRequest1 = new
        {
            Title = "Challenge 1",
            Description = "First challenge",
            SelectionCriteriaStmt = "Criteria 1",
            IsPublished = true,
        };
        var challengeResponse1 = await client1.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest1
        );
        var challenge1 = await challengeResponse1.Content.ReadFromJsonAsync<ChallengeResponse>();

        var challengeRequest2 = new
        {
            Title = "Challenge 2",
            Description = "Second challenge",
            SelectionCriteriaStmt = "Criteria 2",
            IsPublished = true,
        };
        var challengeResponse2 = await client1.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/challenges",
            challengeRequest2
        );
        var challenge2 = await challengeResponse2.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Create second user for team 2
        var client2 = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 9999,
            GitHubLogin = "integration-test-user-2",
            FirstName = "Integration Test User",
            LastName = "2",
            Email = "integration2@example.com",
        };
        await client2.InitializeAsync();

        try
        {
            // Both users join hackathon as participants
            await client1.HttpClient.PostAsync(
                $"/participants/hackathons/{hackathon.Id}/join",
                null
            );
            await client2.HttpClient.PostAsync(
                $"/participants/hackathons/{hackathon.Id}/join",
                null
            );

            // Create two teams (one per user)
            var teamRequest1 = new { Name = "Team 1", Description = "First team" };
            var teamResponse1 = await client1.HttpClient.PostAsJsonAsync(
                $"/participants/hackathons/{hackathon.Id}/teams",
                teamRequest1
            );
            var team1 = await teamResponse1.Content.ReadFromJsonAsync<CreateTeamResponse>();

            var teamRequest2 = new { Name = "Team 2", Description = "Second team" };
            var teamResponse2 = await client2.HttpClient.PostAsJsonAsync(
                $"/participants/hackathons/{hackathon.Id}/teams",
                teamRequest2
            );
            var team2 = await teamResponse2.Content.ReadFromJsonAsync<CreateTeamResponse>();

            // Act & Assert - Initial state: no teams selected challenges
            var challengesResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/challenges"
            );
            var challenges =
                await challengesResponse.Content.ReadFromJsonAsync<ParticipantChallengesListResponse>();
            await Assert.That(challengesResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(challenges!.Challenges).IsNotNull();

            var challenge1Item = challenges.Challenges!.FirstOrDefault(c => c.Id == challenge1!.Id);
            var challenge2Item = challenges.Challenges!.FirstOrDefault(c => c.Id == challenge2!.Id);
            await Assert.That(challenge1Item).IsNotNull();
            await Assert.That(challenge2Item).IsNotNull();
            await Assert.That(challenge1Item.TeamCount).IsEqualTo(0);
            await Assert.That(challenge2Item.TeamCount).IsEqualTo(0);

            // Team 1 selects challenge 1
            var selectResponse1 = await client1.HttpClient.PutAsJsonAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/{team1!.Id}/challenge",
                new { ChallengeId = challenge1!.Id }
            );
            await Assert.That(selectResponse1.StatusCode).IsEqualTo(HttpStatusCode.OK);

            // Verify challenge 1 TeamCount = 1, challenge 2 TeamCount = 0
            challengesResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/challenges"
            );
            challenges =
                await challengesResponse.Content.ReadFromJsonAsync<ParticipantChallengesListResponse>();
            challenge1Item = challenges!.Challenges!.FirstOrDefault(c => c.Id == challenge1!.Id);
            challenge2Item = challenges.Challenges!.FirstOrDefault(c => c.Id == challenge2!.Id);
            await Assert.That(challenge1Item).IsNotNull();
            await Assert.That(challenge2Item).IsNotNull();
            await Assert.That(challenge1Item.TeamCount).IsEqualTo(1);
            await Assert.That(challenge2Item.TeamCount).IsEqualTo(0);

            // Verify team 1 shows challenge 1 as selected
            var team1Response = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/me"
            );
            var team1Result = await team1Response.Content.ReadFromJsonAsync<MyTeamResponse>();
            await Assert.That(team1Result!.ChallengeId).IsEqualTo(challenge1!.Id);

            // Team 2 selects challenge 1
            var selectResponse2 = await client2.HttpClient.PutAsJsonAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/{team2!.Id}/challenge",
                new { ChallengeId = challenge1!.Id }
            );
            await Assert.That(selectResponse2.StatusCode).IsEqualTo(HttpStatusCode.OK);

            // Verify challenge 1 TeamCount = 2, challenge 2 TeamCount = 0
            challengesResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/challenges"
            );
            challenges =
                await challengesResponse.Content.ReadFromJsonAsync<ParticipantChallengesListResponse>();
            challenge1Item = challenges!.Challenges!.FirstOrDefault(c => c.Id == challenge1!.Id);
            challenge2Item = challenges.Challenges!.FirstOrDefault(c => c.Id == challenge2!.Id);
            await Assert.That(challenge1Item).IsNotNull();
            await Assert.That(challenge2Item).IsNotNull();
            await Assert.That(challenge1Item.TeamCount).IsEqualTo(2);
            await Assert.That(challenge2Item.TeamCount).IsEqualTo(0);

            // Verify team 2 shows challenge 1 as selected
            var team2Response = await client2.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/me"
            );
            var team2Result = await team2Response.Content.ReadFromJsonAsync<MyTeamResponse>();
            await Assert.That(team2Result!.ChallengeId).IsEqualTo(challenge1!.Id);

            // Team 1 changes to challenge 2
            var changeResponse = await client1.HttpClient.PutAsJsonAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/{team1!.Id}/challenge",
                new { ChallengeId = challenge2!.Id }
            );
            await Assert.That(changeResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

            // Verify challenge 1 TeamCount = 1, challenge 2 TeamCount = 1
            challengesResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/challenges"
            );
            challenges =
                await challengesResponse.Content.ReadFromJsonAsync<ParticipantChallengesListResponse>();
            challenge1Item = challenges!.Challenges!.FirstOrDefault(c => c.Id == challenge1!.Id);
            challenge2Item = challenges.Challenges!.FirstOrDefault(c => c.Id == challenge2!.Id);
            await Assert.That(challenge1Item).IsNotNull();
            await Assert.That(challenge2Item).IsNotNull();
            await Assert.That(challenge1Item.TeamCount).IsEqualTo(1);
            await Assert.That(challenge2Item.TeamCount).IsEqualTo(1);

            // Verify team 1 shows challenge 2 as selected
            team1Response = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/me"
            );
            team1Result = await team1Response.Content.ReadFromJsonAsync<MyTeamResponse>();
            await Assert.That(team1Result!.ChallengeId).IsEqualTo(challenge2!.Id);

            // Verify team 2 still shows challenge 1 as selected
            team2Response = await client2.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathon.Id}/teams/me"
            );
            team2Result = await team2Response.Content.ReadFromJsonAsync<MyTeamResponse>();
            await Assert.That(team2Result!.ChallengeId).IsEqualTo(challenge1!.Id);
        }
        finally
        {
            await client2.DisposeAsync();
        }
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RemoveMember_AsTeamCreator_ReturnsOk(AuthenticatedHttpClientDataClass client1)
    {
        // Arrange - Create hackathon and team with two members
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client1);

        // Create team (client1 becomes the creator)
        var teamRequest = new { Name = "Test Team", Description = "Team for removal test" };
        var teamResponse = await client1.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Create second user and join the team
        var client2 = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 9998,
            GitHubLogin = "integration-test-user-remove",
            FirstName = "To Be Removed",
            LastName = "User",
            Email = "removed@example.com",
        };
        await client2.InitializeAsync();

        try
        {
            // Second user joins hackathon and team
            await client2.HttpClient.PostAsync(
                $"/participants/hackathons/{hackathonId}/join",
                null
            );
            await client2.HttpClient.PostAsJsonAsync(
                "/participants/teams/join",
                new { team!.JoinCode }
            );

            // Get team to find second user's ID
            var myTeamResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathonId}/teams/me"
            );
            var myTeam = await myTeamResponse.Content.ReadFromJsonAsync<MyTeamResponse>();
            var secondMember = myTeam!.Members!.FirstOrDefault(m => !m.IsCurrentUser);

            // Act - Remove second member as team creator
            var response = await client1.HttpClient.DeleteAsync(
                $"/participants/hackathons/{hackathonId}/teams/{team.Id}/members/{secondMember!.UserId}"
            );
            var result = await response.Content.ReadFromJsonAsync<RemoveMemberResponse>();

            // Assert
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Message).IsNotNull();

            // Verify second member is no longer in team
            myTeamResponse = await client1.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathonId}/teams/me"
            );
            myTeam = await myTeamResponse.Content.ReadFromJsonAsync<MyTeamResponse>();
            await Assert.That(myTeam!.Members!.Count).IsEqualTo(1);
        }
        finally
        {
            await client2.DisposeAsync();
        }
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RemoveMember_AsNonCreator_ReturnsForbidden(
        AuthenticatedHttpClientDataClass client1
    )
    {
        // Arrange - Create hackathon and team
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client1);

        var teamRequest = new { Name = "Test Team", Description = "Team for removal test" };
        var teamResponse = await client1.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Create second user and join the team
        var client2 = new AuthenticatedHttpClientDataClass
        {
            GitHubId = 9997,
            GitHubLogin = "integration-test-user-noncreator",
            FirstName = "Non Creator",
            LastName = "User",
            Email = "noncreator@example.com",
        };
        await client2.InitializeAsync();

        try
        {
            await client2.HttpClient.PostAsync(
                $"/participants/hackathons/{hackathonId}/join",
                null
            );
            await client2.HttpClient.PostAsJsonAsync(
                "/participants/teams/join",
                new { team!.JoinCode }
            );

            // Get team to find creator's ID
            var myTeamResponse = await client2.HttpClient.GetAsync(
                $"/participants/hackathons/{hackathonId}/teams/me"
            );
            var myTeam = await myTeamResponse.Content.ReadFromJsonAsync<MyTeamResponse>();
            var creator = myTeam!.Members!.FirstOrDefault(m => !m.IsCurrentUser);

            // Act - Try to remove creator as non-creator member
            var response = await client2.HttpClient.DeleteAsync(
                $"/participants/hackathons/{hackathonId}/teams/{team.Id}/members/{creator!.UserId}"
            );

            // Assert - Should be forbidden
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        }
        finally
        {
            await client2.DisposeAsync();
        }
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RemoveMember_RemovingSelf_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        var teamRequest = new { Name = "Test Team", Description = "Team for self-removal test" };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Get my team to find my user ID
        var myTeamResponse = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/me"
        );
        var myTeam = await myTeamResponse.Content.ReadFromJsonAsync<MyTeamResponse>();
        var currentUser = myTeam!.Members!.First(m => m.IsCurrentUser);

        // Act - Try to remove self
        var response = await client.HttpClient.DeleteAsync(
            $"/participants/hackathons/{hackathonId}/teams/{team!.Id}/members/{currentUser.UserId}"
        );

        // Assert - Should return error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RemoveMember_NonExistentMember_ReturnsError(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        var teamRequest = new
        {
            Name = "Test Team",
            Description = "Team for non-existent member test",
        };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Act - Try to remove non-existent user
        var response = await client.HttpClient.DeleteAsync(
            $"/participants/hackathons/{hackathonId}/teams/{team!.Id}/members/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task UpdateTeam_CacheInvalidation_ReturnsUpdatedData(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create hackathon, join, and create a team
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);
        var teamRequest = new { Name = "Original Team Name", Description = "Original description" };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            teamRequest
        );
        var team = await createResponse.Content.ReadFromJsonAsync<CreateTeamResponse>();

        // Act 1 - Get team to populate cache
        var response1 = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/me"
        );
        var result1 = await response1.Content.ReadFromJsonAsync<GetMyTeamResponse>();
        await Assert.That(result1).IsNotNull();
        await Assert.That(result1!.Name).IsEqualTo("Original Team Name");

        // Act 2 - Update the team
        var updateRequest = new { Name = "Updated Team Name", Description = "Updated description" };
        var updateResponse = await client.HttpClient.PatchAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams/{team!.Id}",
            updateRequest
        );
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Get team again to verify cache was invalidated
        var response2 = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/teams/me"
        );
        var result2 = await response2.Content.ReadFromJsonAsync<GetMyTeamResponse>();

        // Assert - Should return updated data, not cached old data
        await Assert.That(result2).IsNotNull();
        await Assert.That(result2!.Name).IsEqualTo("Updated Team Name");
        await Assert.That(result2.Description).IsEqualTo("Updated description");
    }
}
