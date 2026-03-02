using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class WithdrawFromHackathonTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Withdraw Test Hackathon {suffix}",
            Description = "A test hackathon for withdraw tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"WITH{suffix}",
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

        await client.HttpClient.PostAsync($"/participants/hackathons/{result!.Id}/join", null);

        return result.Id;
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_WithNoTeam_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/withdraw",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_WhenInTeam_ReturnsBadRequest(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Create a team (participant is auto-assigned to it)
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            new { Name = "Test Team", Description = "Team for withdraw test" }
        );

        // Act - Try to withdraw from hackathon while in a team
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/withdraw",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_AfterLeavingTeam_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Create and then leave the team
        await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            new { Name = "Test Team", Description = "Team for withdraw test" }
        );
        await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/teams/leave",
            null
        );

        // Act - Withdraw from hackathon after leaving team
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/withdraw",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_SetsStatusToNotParticipant(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Verify participant before withdrawing
        var beforeWithdrawStatus = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var beforeStatus =
            await beforeWithdrawStatus.Content.ReadFromJsonAsync<ParticipantStatusResponse>();
        await Assert.That(beforeStatus!.IsParticipant).IsTrue();

        // Act
        await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/withdraw",
            null
        );

        // Assert
        var afterWithdrawStatus = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var afterStatus =
            await afterWithdrawStatus.Content.ReadFromJsonAsync<ParticipantStatusResponse>();
        await Assert.That(afterStatus!.IsParticipant).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_ThenRejoin_ReturnsParticipant(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAndJoinAsync(client);

        // Withdraw from hackathon
        await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/withdraw",
            null
        );

        // Verify not participant
        var afterWithdrawStatus = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var afterWithdraw =
            await afterWithdrawStatus.Content.ReadFromJsonAsync<ParticipantStatusResponse>();
        await Assert.That(afterWithdraw!.IsParticipant).IsFalse();

        // Act - Rejoin hackathon
        var rejoinResponse = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        await Assert.That(rejoinResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Assert - Should be participant again
        var afterRejoinStatus = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var afterRejoin =
            await afterRejoinStatus.Content.ReadFromJsonAsync<ParticipantStatusResponse>();
        await Assert.That(afterRejoin!.IsParticipant).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task WithdrawFromHackathon_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/withdraw",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task WithdrawFromHackathon_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/withdraw",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
