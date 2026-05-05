using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class OrganizersManagementTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass AnonymousClient { get; init; }

    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Organizers Test Hackathon {suffix}",
            Description = "A test hackathon for organizers tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"ORGN{suffix}",
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
    public async Task ListOrganizers_WithValidHackathon_ReturnsOk()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(Client);

        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathonId}/organizers"
        );
        var result = await response.Content.ReadFromJsonAsync<OrganizersListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Organizers).IsNotNull();
        // Creator should be an organizer
        await Assert.That(result.Organizers!.Count()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ListOrganizers_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await Client.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers"
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ListOrganizers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.GetAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateInvite_WithValidHackathon_ReturnsInviteCode()
    {
        // Arrange
        var hackathonId = await CreateHackathonAsync(Client);

        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/organizers/invites",
            new { Type = "Volunteer" }
        );
        var result = await response.Content.ReadFromJsonAsync<OrganizerInviteResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Code).IsNotNull();
        await Assert.That(result.Code.Length).IsEqualTo(8);
        await Assert.That(result.ExpiresAt).IsGreaterThan(DateTimeOffset.UtcNow);
    }

    [Test]
    public async Task CreateInvite_WithInvalidHackathonId_ReturnsNotFound()
    {
        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers/invites",
            new { Type = "Volunteer" }
        );

        // Assert
        await Assert
            .That(response.StatusCode)
            .IsEqualTo(HttpStatusCode.NotFound)
            .Or.IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateInvite_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{Guid.NewGuid()}/organizers/invites",
            new { Type = "Volunteer" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AcceptInvite_WithInvalidCode_ReturnsError()
    {
        // Act
        var response = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/accept-invite",
            new { Code = "INVALID1" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AcceptInvite_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousClient.HttpClient.PostAsJsonAsync(
            "/organizers/accept-invite",
            new { Code = "SOMECODE" }
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

