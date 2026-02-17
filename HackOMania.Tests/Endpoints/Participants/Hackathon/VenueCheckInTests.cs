using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class VenueCheckInTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Venue Test {suffix}",
            Description = "A test hackathon for venue check-in tests",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"VNT{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient httpClient)
    {
        var whoami = await httpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
        return whoami!.Id;
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CheckIn_ShouldSucceed(AuthenticatedHttpClientDataClass client)
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon as participant
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Check in
        var checkInResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/venue/check-in",
            new { }
        );

        await Assert.That(checkInResponse.IsSuccessStatusCode).IsTrue();
        var result = await checkInResponse.Content.ReadFromJsonAsync<CheckInResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCheckedIn).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CheckOut_ShouldSucceed(AuthenticatedHttpClientDataClass client)
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Check in first
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/venue/check-in",
            new { }
        );

        // Check out
        var checkOutResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/venue/check-out",
            new { }
        );

        await Assert.That(checkOutResponse.IsSuccessStatusCode).IsTrue();
        var result = await checkOutResponse.Content.ReadFromJsonAsync<CheckOutResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCheckedIn).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task CheckOut_WithoutCheckIn_ShouldFail(AuthenticatedHttpClientDataClass client)
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        // Try to check out without checking in
        var checkOutResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/venue/check-out",
            new { }
        );

        await Assert.That(checkOutResponse.IsSuccessStatusCode).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task OrganizerOverview_ShouldShowCheckInStatus(
        AuthenticatedHttpClientDataClass client
    )
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Join hackathon
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);

        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Check in
        await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/venue/check-in",
            new { }
        );

        // Get overview as organizer
        var overviewResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/venue/overview"
        );

        await Assert.That(overviewResponse.IsSuccessStatusCode).IsTrue();
        var result = await overviewResponse.Content.ReadFromJsonAsync<VenueOverviewResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Participants.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(result.Participants.Any(p => p.IsCurrentlyCheckedIn)).IsTrue();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task OrganizerOverview_CacheInvalidation_AfterCheckOut_ReturnsNotCheckedIn(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Check in first
        var checkInResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/venue/check-in",
            new { }
        );
        await Assert.That(checkInResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 1 - Warm overview cache while participant is checked in
        var beforeCheckOutResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/venue/overview"
        );
        var beforeCheckOut =
            await beforeCheckOutResponse.Content.ReadFromJsonAsync<VenueOverviewResponse>();
        await Assert.That(beforeCheckOutResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(beforeCheckOut).IsNotNull();
        var beforeParticipant = beforeCheckOut!.Participants.FirstOrDefault(p =>
            p.UserId == participantUserId
        );
        await Assert.That(beforeParticipant).IsNotNull();
        await Assert.That(beforeParticipant!.IsCurrentlyCheckedIn).IsTrue();

        // Act 2 - Check out participant
        var checkOutResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/venue/check-out",
            new { }
        );
        await Assert.That(checkOutResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act 3 - Read overview again and verify cache invalidation
        var afterCheckOutResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/venue/overview"
        );
        var afterCheckOut =
            await afterCheckOutResponse.Content.ReadFromJsonAsync<VenueOverviewResponse>();

        // Assert - Should now show checked-out state
        await Assert.That(afterCheckOutResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(afterCheckOut).IsNotNull();
        var afterParticipant = afterCheckOut!.Participants.FirstOrDefault(p =>
            p.UserId == participantUserId
        );
        await Assert.That(afterParticipant).IsNotNull();
        await Assert.That(afterParticipant!.IsCurrentlyCheckedIn).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task OrganizerOverview_ConcurrentCheckInAndReads_FinalReadShowsCheckedIn(
        AuthenticatedHttpClientDataClass client
    )
    {
        // Arrange
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon!.Id}/join", null);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        // Warm cache with initial not-checked-in state
        var warmupResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/venue/overview"
        );
        await Assert.That(warmupResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act - Check in while issuing concurrent reads
        var checkInTask = client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/venue/check-in",
            new { }
        );
        var overviewTasks = Enumerable
            .Range(0, 6)
            .Select(_ => client.HttpClient.GetAsync($"/organizers/hackathons/{hackathon.Id}/venue/overview"))
            .ToList();

        await Task.WhenAll(overviewTasks.Append(checkInTask));

        // Assert intermediate calls succeeded
        await Assert.That(checkInTask.Result.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var allOverviewReadsOk = overviewTasks.All(t => t.Result.StatusCode == HttpStatusCode.OK);
        await Assert.That(allOverviewReadsOk).IsTrue();

        // Final assert - eventual state must show participant checked in
        var finalOverviewResponse = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/venue/overview"
        );
        var finalOverview =
            await finalOverviewResponse.Content.ReadFromJsonAsync<VenueOverviewResponse>();
        await Assert.That(finalOverviewResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(finalOverview).IsNotNull();
        var participant = finalOverview!.Participants.FirstOrDefault(p => p.UserId == participantUserId);
        await Assert.That(participant).IsNotNull();
        await Assert.That(participant!.IsCurrentlyCheckedIn).IsTrue();
    }
}

public class CheckInResponse
{
    public Guid Id { get; set; }
    public DateTimeOffset CheckInTime { get; set; }
    public bool IsCheckedIn { get; set; }
}

public class CheckOutResponse
{
    public Guid Id { get; set; }
    public DateTimeOffset CheckOutTime { get; set; }
    public bool IsCheckedIn { get; set; }
}

public class VenueOverviewResponse
{
    public List<ParticipantCheckInDto> Participants { get; set; } = new();
}

public class ParticipantCheckInDto
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public bool IsCurrentlyCheckedIn { get; set; }
    public DateTimeOffset? LastCheckInTime { get; set; }
    public DateTimeOffset? LastCheckOutTime { get; set; }
    public int TotalCheckIns { get; set; }
}
