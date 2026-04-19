using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class ParticipantsManagementTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass organizerClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Participants Mgmt Test Hackathon {suffix}",
            Description = "A test hackathon for participants management tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PRMG{suffix}",
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

    private static async Task<AuthenticatedHttpClientDataClass> CreateParticipantClientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var gitHubId = Math.Abs(BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0));
        if (gitHubId == 0)
        {
            gitHubId = 1;
        }

        var client = new AuthenticatedHttpClientDataClass
        {
            GitHubId = gitHubId,
            GitHubLogin = $"participant-{suffix}",
            FirstName = "Participant",
            LastName = suffix,
            Email = $"participant-{suffix}@example.com",
        };

        await client.InitializeAsync();
        return client;
    }

    private static async Task<(
        Guid HackathonId,
        Guid ParticipantUserId
    )> CreateHackathonWithJoinedParticipantAsync(AuthenticatedHttpClientDataClass organizerClient)
    {
        var hackathonId = await CreateHackathonAsync(organizerClient);

        await using var participantClient = await CreateParticipantClientAsync();
        var participantWhoAmI = await participantClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>(
            "/auth/whoami"
        );
        await Assert.That(participantWhoAmI).IsNotNull();

        var joinResponse = await participantClient.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        await Assert.That(joinResponse.IsSuccessStatusCode).IsTrue();

        return (hackathonId, participantWhoAmI!.Id);
    }

    [Test]
    public async Task ListParticipants_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Participants).IsNotNull();
    }

    [Test]
    public async Task ListParticipants_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListParticipants_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await anonymousClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ReviewParticipant_WithConcurrentRequests_BothReviewsAreSaved()
    {
        // Arrange
        var (hackathonId, participantUserId) = await CreateHackathonWithJoinedParticipantAsync(
            organizerClient
        );

        var reviewRequest = new ParticipantReviewRequest
        {
            Decision = "accept",
            Reason = "Concurrent review safety test",
        };
        var reviewUrl =
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/review";

        // Act
        var reviewTask1 = organizerClient.HttpClient.PostAsJsonAsync(reviewUrl, reviewRequest);
        var reviewTask2 = organizerClient.HttpClient.PostAsJsonAsync(reviewUrl, reviewRequest);
        await Task.WhenAll(reviewTask1, reviewTask2);

        var statuses = new[] { reviewTask1.Result.StatusCode, reviewTask2.Result.StatusCode };

        // Assert - Both requests should succeed as row locking serializes them,
        // allowing both reviews to be saved sequentially
        await Assert.That(statuses.Count(s => s == HttpStatusCode.OK)).IsEqualTo(2);

        var listResponse = await organizerClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var participants = await listResponse.Content.ReadFromJsonAsync<ParticipantsListResponse>();
        await Assert.That(participants).IsNotNull();

        var reviewedParticipant = participants!.Participants?.FirstOrDefault(p =>
            p.Id == participantUserId
        );
        await Assert.That(reviewedParticipant).IsNotNull();
        await Assert.That(reviewedParticipant!.Reviews).IsNotNull();
        // Both reviews should be saved - row locking ensures serialization but allows both
        await Assert.That(reviewedParticipant.Reviews!.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task ReviewParticipant_WithRejectDecisionWithoutReason_ReturnsBadRequest()
    {
        // Arrange
        var (hackathonId, participantUserId) = await CreateHackathonWithJoinedParticipantAsync(
            organizerClient
        );
        var reviewRequest = new ParticipantReviewRequest { Decision = "reject", Reason = "   " };

        // Act
        var response = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/review",
            reviewRequest
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ReviewParticipant_WithAcceptDecisionWithoutReason_ReturnsOk()
    {
        // Arrange
        var (hackathonId, participantUserId) = await CreateHackathonWithJoinedParticipantAsync(
            organizerClient
        );
        var reviewRequest = new ParticipantReviewRequest { Decision = "accept", Reason = null };

        // Act
        var response = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/review",
            reviewRequest
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantReviewResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Status).IsEqualTo("Accepted");
    }

    [Test]
    public async Task ListParticipants_CacheInvalidation_AfterReview_ReflectsAcceptedState()
    {
        // Arrange
        var (hackathonId, participantUserId) = await CreateHackathonWithJoinedParticipantAsync(
            organizerClient
        );

        // Act 1 - Warm participants list cache before review
        var beforeReviewResponse = await organizerClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var beforeReview =
            await beforeReviewResponse.Content.ReadFromJsonAsync<ParticipantsListResponse>();
        await Assert.That(beforeReviewResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(beforeReview).IsNotNull();

        // Act 2 - Review participant as accepted
        var reviewRequest = new ParticipantReviewRequest
        {
            Decision = "accept",
            Reason = "Cache invalidation test",
        };
        var reviewResponse = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantUserId}/review",
            reviewRequest
        );
        await Assert.That(reviewResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Read participants list again
        var afterReviewResponse = await organizerClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var afterReview =
            await afterReviewResponse.Content.ReadFromJsonAsync<ParticipantsListResponse>();

        // Assert - Must reflect accepted review (not stale pending cache)
        await Assert.That(afterReviewResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(afterReview).IsNotNull();
        var reviewedParticipant = afterReview!.Participants?.FirstOrDefault(p =>
            p.Id == participantUserId
        );
        await Assert.That(reviewedParticipant).IsNotNull();
        await Assert.That(reviewedParticipant!.ConcludedStatus).IsEqualTo("Accepted");
        await Assert.That(afterReview.AcceptedCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ParticipantStatus_CacheInvalidation_AfterReview_ReturnsAccepted()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(organizerClient);
        await using var participantClient = await CreateParticipantClientAsync();
        var participantWhoAmI = await participantClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>(
            "/auth/whoami"
        );
        await Assert.That(participantWhoAmI).IsNotNull();

        var joinResponse = await participantClient.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        await Assert.That(joinResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 1 - Warm participant status cache before review
        var beforeReviewStatusResponse = await participantClient.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var beforeReviewStatus =
            await beforeReviewStatusResponse.Content.ReadFromJsonAsync<ParticipantStatusResponse>();
        await Assert.That(beforeReviewStatusResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(beforeReviewStatus).IsNotNull();
        await Assert.That(beforeReviewStatus!.Status).IsEqualTo("Pending");

        // Act 2 - Organizer reviews participant as accepted
        var reviewResponse = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/participants/{participantWhoAmI!.Id}/review",
            new ParticipantReviewRequest { Decision = "accept", Reason = "Status cache test" }
        );
        await Assert.That(reviewResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Participant checks status again
        var afterReviewStatusResponse = await participantClient.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var afterReviewStatus =
            await afterReviewStatusResponse.Content.ReadFromJsonAsync<ParticipantStatusResponse>();

        // Assert - Must reflect accepted status, not stale pending cache
        await Assert.That(afterReviewStatusResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(afterReviewStatus).IsNotNull();
        await Assert.That(afterReviewStatus!.Status).IsEqualTo("Accepted");
    }

    [Test]
    public async Task BatchEmail_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Arrange
        var request = new BatchEmailRequest { Status = "All" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants/batch-email",
            request
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task BatchEmail_WithValidHackathonNoParticipants_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(client);
        var request = new BatchEmailRequest { Status = "All" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/participants/batch-email",
            request
        );
        var result = await response.Content.ReadFromJsonAsync<BatchEmailResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TotalEmailsSent).IsEqualTo(0);
    }

    [Test]
    public async Task BatchEmail_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new BatchEmailRequest { Status = "All" };

        // Act
        var response = await anonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants/batch-email",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task BanUser_WithOrganizerAccess_BlocksAuthenticatedRequestsAndMarksParticipant()
    {
        var hackathonId = await CreateHackathonAsync(organizerClient);
        await using var participantClient = await CreateParticipantClientAsync();

        var participantWhoAmI = await participantClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>(
            "/auth/whoami"
        );
        await Assert.That(participantWhoAmI).IsNotNull();

        var joinResponse = await participantClient.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        await Assert.That(joinResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var banResponse = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/users/{participantWhoAmI!.Id}/ban",
            new { Reason = "Silent moderation test" }
        );
        var banState = await banResponse.Content.ReadFromJsonAsync<UserBanStateResponse>();

        await Assert.That(banResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(banState).IsNotNull();
        await Assert.That(banState!.IsBanned).IsTrue();
        await Assert.That(banState.BanReason).IsEqualTo("Silent moderation test");

        var bannedWhoAmIResponse = await participantClient.HttpClient.GetAsync("/auth/whoami");
        var bannedHackathonsResponse = await participantClient.HttpClient.GetAsync(
            "/participants/hackathons"
        );

        await Assert.That(bannedWhoAmIResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(bannedHackathonsResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var listResponse = await organizerClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var participants = await listResponse.Content.ReadFromJsonAsync<ParticipantsListResponse>();
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(participants).IsNotNull();

        var bannedParticipant = participants!.Participants?.FirstOrDefault(p =>
            p.Id == participantWhoAmI.Id
        );
        await Assert.That(bannedParticipant).IsNotNull();
        await Assert.That(bannedParticipant!.IsBanned).IsTrue();
        await Assert.That(bannedParticipant.BanReason).IsEqualTo("Silent moderation test");
    }

    [Test]
    public async Task UnbanUser_RestoresAuthenticatedAccess()
    {
        await using var participantClient = await CreateParticipantClientAsync();
        var participantWhoAmI = await participantClient.HttpClient.GetFromJsonAsync<WhoAmIResponse>(
            "/auth/whoami"
        );
        await Assert.That(participantWhoAmI).IsNotNull();

        var banResponse = await organizerClient.HttpClient.PostAsJsonAsync(
            $"/organizers/users/{participantWhoAmI!.Id}/ban",
            new { Reason = "Temporary ban test" }
        );
        await Assert.That(banResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var bannedWhoAmIResponse = await participantClient.HttpClient.GetAsync("/auth/whoami");
        await Assert.That(bannedWhoAmIResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        var unbanResponse = await organizerClient.HttpClient.DeleteAsync(
            $"/organizers/users/{participantWhoAmI.Id}/ban"
        );
        var unbanState = await unbanResponse.Content.ReadFromJsonAsync<UserBanStateResponse>();

        await Assert.That(unbanResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(unbanState).IsNotNull();
        await Assert.That(unbanState!.IsBanned).IsFalse();

        var restoredWhoAmIResponse = await participantClient.HttpClient.GetAsync("/auth/whoami");
        var restoredWhoAmI =
            await restoredWhoAmIResponse.Content.ReadFromJsonAsync<WhoAmIResponse>();

        await Assert.That(restoredWhoAmIResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(restoredWhoAmI).IsNotNull();
        await Assert.That(restoredWhoAmI!.Id).IsEqualTo(participantWhoAmI.Id);
    }
}

