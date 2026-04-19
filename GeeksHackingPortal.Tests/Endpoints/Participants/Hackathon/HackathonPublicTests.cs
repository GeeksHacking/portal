using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;

public class HackathonPublicTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass anonymousClient { get; init; }

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
    public async Task ListPublicHackathons_WithoutAuthentication_ReturnsOk()
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
    public async Task ListPublicHackathons_OnlyReturnsPublishedHackathons()
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
    public async Task GetPublicHackathon_ByIdWithPublished_ReturnsOk()
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
    public async Task GetPublicHackathon_ByShortCode_ReturnsOk()
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
    public async Task GetPublicHackathon_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{Guid.NewGuid()}"
        );

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetPublicHackathon_UnpublishedHackathon_ReturnsNotFound()
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

    [Test]
    public async Task ListPublicHackathons_CacheInvalidation_AfterPublish_IncludesHackathon()
    {
        // Arrange - Create unpublished hackathon
        var request = CreateValidHackathonRequest(
            Guid.NewGuid().ToString()[..8],
            isPublished: false
        );
        var createResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            request
        );
        var created = await createResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await Assert.That(created).IsNotNull();

        // Act 1 - Warm public list cache before publishing
        var beforePublishResponse = await client.HttpClient.GetAsync("/participants/hackathons");
        var beforePublish =
            await beforePublishResponse.Content.ReadFromJsonAsync<ParticipantsHackathonsListResponse>();
        await Assert.That(beforePublishResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(beforePublish).IsNotNull();
        var existsBeforePublish = beforePublish!.Hackathons!.Any(h => h.Id == created!.Id);
        await Assert.That(existsBeforePublish).IsFalse();

        // Act 2 - Publish hackathon
        var publishResponse = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{created.Id}",
            new UpdateHackathonRequest { IsPublished = true }
        );
        await Assert.That(publishResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Read public list again
        var afterPublishResponse = await client.HttpClient.GetAsync("/participants/hackathons");
        var afterPublish =
            await afterPublishResponse.Content.ReadFromJsonAsync<ParticipantsHackathonsListResponse>();

        // Assert - Newly published hackathon should now be visible
        await Assert.That(afterPublishResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(afterPublish).IsNotNull();
        var existsAfterPublish = afterPublish!.Hackathons!.Any(h => h.Id == created.Id);
        await Assert.That(existsAfterPublish).IsTrue();
    }

    [Test]
    public async Task GetPublicHackathon_CacheInvalidation_AfterUnpublish_ReturnsNotFound()
    {
        // Arrange - Create published hackathon
        var created = await CreatePublishedHackathonAsync(client);

        // Act 1 - Warm cache via public get
        var warmupResponse = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{created.Id}"
        );
        await Assert.That(warmupResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 2 - Unpublish hackathon
        var unpublishResponse = await client.HttpClient.PatchAsJsonAsync(
            $"/organizers/hackathons/{created.Id}",
            new UpdateHackathonRequest { IsPublished = false }
        );
        await Assert.That(unpublishResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Public get should no longer expose this hackathon
        var afterUnpublishResponse = await client.HttpClient.GetAsync(
            $"/participants/hackathons/{created.Id}"
        );

        // Assert
        await Assert.That(afterUnpublishResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

