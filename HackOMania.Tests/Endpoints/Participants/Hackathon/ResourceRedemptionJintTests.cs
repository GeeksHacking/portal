using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class ResourceRedemptionJintTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Resource Jint Test {suffix}",
            Description = "A test hackathon for resource redemption with Jint",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"RJT{suffix}",
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
    public async Task RedeemResource_WithParticipantRedemptionLimit_ShouldEnforce(
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

        // Create resource with redemption limit of 2 per participant
        var resourceRequest = new
        {
            Name = "Limited Swag",
            Description = "Max 2 per participant",
            RedemptionStmt = "participantRedemptions < 2",
            IsPublished = true,
        };

        var createResourceResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/resources",
            resourceRequest
        );
        var resource = await createResourceResponse.Content.ReadFromJsonAsync<ResourceResponse>();

        // First redemption - should succeed
        var redemption1 = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource!.Id}/redemptions",
            null
        );
        await Assert.That(redemption1.IsSuccessStatusCode).IsTrue();

        // Second redemption - should succeed
        var redemption2 = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );
        await Assert.That(redemption2.IsSuccessStatusCode).IsTrue();

        // Third redemption - should fail
        var redemption3 = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );
        await Assert.That(redemption3.IsSuccessStatusCode).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task RedeemResource_WithTeamSizeRequirement_ShouldEnforce(
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

        // Create resource that requires team size >= 3
        var resourceRequest = new
        {
            Name = "Team Resource",
            Description = "Requires team of 3+",
            RedemptionStmt = "hasTeam && teamSize >= 3",
            IsPublished = true,
        };

        var createResourceResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/resources",
            resourceRequest
        );
        var resource = await createResourceResponse.Content.ReadFromJsonAsync<ResourceResponse>();

        // Try to redeem without team - should fail
        var redemption = await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource!.Id}/redemptions",
            null
        );
        await Assert.That(redemption.IsSuccessStatusCode).IsFalse();
    }
}

public class ResourceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string RedemptionStmt { get; set; } = null!;
    public bool IsPublished { get; set; }
}
