using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class HackathonPublicTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(
        string suffix = "",
        bool isPublished = true
    )
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Public Test Hackathon {suffix}",
            Description = "A test hackathon for public tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"PUB{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = isPublished,
        };
    }

    private static async Task<HackathonResponse> CreatePublishedHackathonAsync(
        AuthenticatedHttpClientDataClass client
    )
    {
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var response = await client.HttpClient.PostAsJsonAsync("/organizers/hackathons", request);
        return (await response.Content.ReadFromJsonAsync<HackathonResponse>())!;
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task ListPublicHackathons_WithoutAuthentication_ReturnsOk(
        HttpClientDataClass client
    )
    {
        // Act
        var response = await client.HttpClient.GetAsync("/participants/hackathons");
        var result = await response.Content.ReadFromJsonAsync<ParticipantsHackathonsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Hackathons).IsNotNull();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task ListPublicHackathons_OnlyReturnsPublishedHackathons(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a published hackathon
        var hackathon = await CreatePublishedHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync("/participants/hackathons");
        var result = await response.Content.ReadFromJsonAsync<ParticipantsHackathonsListResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Hackathons).IsNotNull();

        // All returned hackathons should be published
        var allPublished = result.Hackathons!.All(h => h.IsPublished);
        await Assert.That(allPublished).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetPublicHackathon_ByIdWithPublished_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a published hackathon
        var hackathon = await CreatePublishedHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync($"/participants/hackathons/{hackathon.Id}");
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(hackathon.Id);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetPublicHackathon_ByShortCode_ReturnsOk(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create a published hackathon
        var hackathon = await CreatePublishedHackathonAsync(client);

        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon.ShortCode}"
        );
        var result = await response.Content.ReadFromJsonAsync<HackathonResponse>();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShortCode).IsEqualTo(hackathon.ShortCode);
    }

    [Test]
    [ClassDataSource<HttpClientDataClass>]
    public async Task GetPublicHackathon_WithInvalidId_ReturnsNotFound(HttpClientDataClass client)
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task GetPublicHackathon_UnpublishedHackathon_ReturnsNotFound(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange - Create an unpublished hackathon
        var request = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8], false);
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            request
        );
        var hackathon = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Act - Try to get it via the public endpoint
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{hackathon!.Id}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
