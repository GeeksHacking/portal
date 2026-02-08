using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Organizers.Hackathon;

public class ParticipantsManagementTests
{
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

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListParticipants_WithValidHackathon_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListParticipants_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListParticipants_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ReviewParticipant_WithConcurrentRequests_OnlyOneReviewIsSaved(
        AuthenticatedHttpClientDataClass organizerClient
    )
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
        await Assert.That(joinResponse.IsSuccessStatusCode).IsTrue();

        var reviewRequest = new ParticipantReviewRequest
        {
            Decision = "accept",
            Reason = "Concurrent review safety test",
        };
        var reviewUrl =
            $"/organizers/hackathons/{hackathonId}/participants/{participantWhoAmI!.Id}/review";

        // Act
        var reviewTask1 = organizerClient.HttpClient.PostAsJsonAsync(reviewUrl, reviewRequest);
        var reviewTask2 = organizerClient.HttpClient.PostAsJsonAsync(reviewUrl, reviewRequest);
        await Task.WhenAll(reviewTask1, reviewTask2);

        var statuses = new[] { reviewTask1.Result.StatusCode, reviewTask2.Result.StatusCode };

        // Assert
        await Assert.That(statuses.Count(s => s == HttpStatusCode.OK)).IsEqualTo(1);
        await Assert.That(statuses.Count(s => s == HttpStatusCode.Conflict)).IsEqualTo(1);

        var listResponse = await organizerClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/participants"
        );
        var participants = await listResponse.Content.ReadFromJsonAsync<ParticipantsListResponse>();
        await Assert.That(participants).IsNotNull();

        var reviewedParticipant = participants!.Participants?.FirstOrDefault(p =>
            p.Id == participantWhoAmI.Id
        );
        await Assert.That(reviewedParticipant).IsNotNull();
        await Assert.That(reviewedParticipant!.Reviews).IsNotNull();
        await Assert.That(reviewedParticipant.Reviews!.Count()).IsEqualTo(1);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task BatchEmail_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task BatchEmail_WithValidHackathonNoParticipants_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
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
    [ClassDataSource<HttpClientDataClass>]
    public async Task BatchEmail_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Arrange
        var request = new BatchEmailRequest { Status = "All" };

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/participants/batch-email",
            request
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
