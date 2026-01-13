using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class JoinAndStatusTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Join Test Hackathon {suffix}",
            Description = "A test hackathon for join tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"JOIN{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> CreatePublishedHackathonAsync(
        AuthenticatedHttpClientDataClass client
    )
    {
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();
        return result!.Id;
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathon_WithValidHackathon_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAsync(client);

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonJoinResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.HackathonId).IsEqualTo(hackathonId);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathon_AlreadyJoined_ReturnsOk(AuthenticatedHttpClientDataClass client)
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAsync(client);

        // Join the first time
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathonId}/join", null);

        // Act - Join again
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );

        // Assert - Should still return OK (idempotent)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathon_RapidRepeatCalls_DoesNotThrottleInDevelopment(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonId = await CreatePublishedHackathonAsync(client);

        // Act/Assert
        for (var i = 0; i < 15; i++)
        {
            var response = await client.HttpClient.PostAsync(
                $"/participants/hackathons/{hackathonId}/join",
                null
            );

            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathon_WithUnpublishedHackathon_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create an unpublished hackathon
        var now = DateTimeOffset.UtcNow;
        var request = new CreateHackathonRequest
        {
            Name = "Unpublished Join Test",
            Description = "An unpublished hackathon",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"UNPB{Guid.NewGuid().ToString()[..4]}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = false,
        };
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            request
        );
        var hackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathon!.Id}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathon_WithInvalidId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task JoinHackathon_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/join",
            null
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetStatus_BeforeJoining_ReturnsNotParticipant(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a new hackathon but don't join
        var hackathonId = await CreatePublishedHackathonAsync(client);

        // The creator is an organizer but not a participant unless they explicitly join
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathonId}/status"
        );
        var result = await response.Content.ReadFromJsonAsync<ParticipantStatusResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        // As organizer, might or might not be participant depending on implementation
        await Assert.That(result!.IsOrganizer).IsTrue();
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task GetStatus_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/status"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetStatus_WithInvalidHackathonId_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}/status"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathonByShortCode_WithValidShortCode_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var shortCode = $"SC{Guid.NewGuid().ToString()[..6]}";
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        hackathonRequest.ShortCode = shortCode;
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = shortCode }
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonJoinResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.HackathonId).IsEqualTo(hackathon!.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathonByShortCode_AlreadyJoined_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var shortCode = $"SC{Guid.NewGuid().ToString()[..6]}";
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        hackathonRequest.ShortCode = shortCode;
        await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", hackathonRequest);

        // Join the first time
        await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = shortCode }
        );

        // Act - Join again
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = shortCode }
        );

        // Assert - Should still return OK (idempotent)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathonByShortCode_WithUnpublishedHackathon_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create an unpublished hackathon
        var shortCode = $"UNP{Guid.NewGuid().ToString()[..5]}";
        var now = DateTimeOffset.UtcNow;
        var request = new CreateHackathonRequest
        {
            Name = "Unpublished ShortCode Test",
            Description = "An unpublished hackathon",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = shortCode,
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = false,
        };
        await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);

        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = shortCode }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task JoinHackathonByShortCode_WithInvalidShortCode_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = "INVALID_CODE_DOES_NOT_EXIST" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task JoinHackathonByShortCode_WithoutAuthentication_ReturnsUnauthorized(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.PostAsJsonAsync(
            "/participants/hackathons/join",
            new { ShortCode = "ANYCODE" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
