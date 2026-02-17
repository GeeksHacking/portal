using System.Net.Http.Json;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Helpers;

/// <summary>
/// Helper class for creating test data consistently across tests
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Creates a valid hackathon request with unique identifiers
    /// </summary>
    public static CreateHackathonRequest CreateValidHackathonRequest(
        string? suffix = null,
        bool isPublished = true
    )
    {
        suffix ??= Guid.NewGuid().ToString()[..8];
        var now = DateTimeOffset.UtcNow;

        return new CreateHackathonRequest
        {
            Name = $"Test Hackathon {suffix}",
            Description = $"A test hackathon created for integration tests ({suffix})",
            Venue = "Virtual Test Venue",
            HomepageUri = new Uri($"https://example.com/hackathon-{suffix}"),
            ShortCode = $"T{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = isPublished,
        };
    }

    /// <summary>
    /// Creates a hackathon and returns its response
    /// </summary>
    public static async Task<HackathonResponse> CreateHackathonAsync(
        HttpClient client,
        bool isPublished = true
    )
    {
        var request = CreateValidHackathonRequest(isPublished: isPublished);
        var response = await client.PostAsJsonAsync("/organizers/hackathons", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HackathonResponse>())!;
    }

    /// <summary>
    /// Creates a hackathon and joins it as a participant
    /// </summary>
    public static async Task<HackathonResponse> CreateHackathonAndJoinAsync(HttpClient client)
    {
        var hackathon = await CreateHackathonAsync(client);
        await client.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        return hackathon;
    }

    /// <summary>
    /// Creates a challenge for a hackathon
    /// </summary>
    public static async Task<ChallengeResponse> CreateChallengeAsync(
        HttpClient client,
        Guid hackathonId,
        bool isPublished = true
    )
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new
        {
            Title = $"Test Challenge {suffix}",
            Description = $"A test challenge for integration tests ({suffix})",
            SelectionCriteriaStmt = "Innovation and technical excellence",
            IsPublished = isPublished,
        };

        var response = await client.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/challenges",
            request
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ChallengeResponse>())!;
    }

    /// <summary>
    /// Creates a team for a hackathon (must have already joined as participant)
    /// </summary>
    public static async Task<CreateTeamResponse> CreateTeamAsync(
        HttpClient client,
        Guid hackathonId
    )
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new
        {
            Name = $"Test Team {suffix}",
            Description = $"A test team for integration tests ({suffix})",
        };

        var response = await client.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/teams",
            request
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;
    }

    /// <summary>
    /// Creates a resource for a hackathon
    /// </summary>
    public static async Task<ResourceResponse> CreateResourceAsync(
        HttpClient client,
        Guid hackathonId
    )
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new
        {
            Name = $"Test Resource {suffix}",
            Description = $"A test resource for integration tests ({suffix})",
            RedemptionStmt = "return true;", // Always allow redemption in tests
        };

        var response = await client.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/resources",
            request
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ResourceResponse>())!;
    }

    /// <summary>
    /// Creates a judge for a hackathon
    /// </summary>
    public static async Task<JudgeResponse> CreateJudgeAsync(HttpClient client, Guid hackathonId)
    {
        var suffix = Guid.NewGuid().ToString()[..8];
        var request = new { Name = $"Test Judge {suffix}" };

        var response = await client.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/judges",
            request
        );
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JudgeResponse>())!;
    }
}
