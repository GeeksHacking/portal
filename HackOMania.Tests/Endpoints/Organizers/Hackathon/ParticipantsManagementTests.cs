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
