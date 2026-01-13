using System.Net.Http.Json;
using HackOMania.Tests.Data;
using HackOMania.Tests.Models;

namespace HackOMania.Tests.Endpoints.Participants.Hackathon;

public class ChallengeSelectionJintTests
{
    private static CreateHackathonRequest CreateValidHackathonRequest(string suffix = "")
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateHackathonRequest
        {
            Name = $"Challenge Jint Test {suffix}",
            Description = "A test hackathon for challenge selection with Jint",
            Venue = "Virtual",
            HomepageUri = new Uri("https://example.com/hackathon"),
            ShortCode = $"CJT{suffix}",
            EventStartDate = now.AddDays(7),
            EventEndDate = now.AddDays(9),
            SubmissionsStartDate = now.AddDays(7).AddHours(2),
            SubmissionsEndDate = now.AddDays(8).AddHours(20),
            JudgingStartDate = now.AddDays(8).AddHours(21),
            JudgingEndDate = now.AddDays(9).AddHours(-2),
            IsPublished = true,
        };
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SubmitChallenge_WithTeamSizeRequirement_ShouldEnforce(
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

        // Create team
        var teamRequest = new { Name = "Solo Team", Description = "A one-person team" };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();

        // Create challenge that requires team size >= 3
        var challengeRequest = new
        {
            Title = "Team Challenge",
            Description = "Requires team of 3+",
            SelectionCriteriaStmt = "teamSize >= 3",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Try to submit with team of 1 - should fail
        var submissionRequest = new
        {
            ChallengeId = challenge!.Id,
            Title = "My Submission",
            Summary = "Test submission",
            RepoUri = "https://github.com/test/repo",
            DemoUri = "https://demo.example.com",
            SlidesUri = "https://slides.example.com",
        };

        var submission = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team!.Id}/submissions",
            submissionRequest
        );

        await Assert.That(submission.IsSuccessStatusCode).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SubmitChallenge_WithMaxTeamsLimit_ShouldEnforce(
        AuthenticatedHttpClientDataClass client
    )
    {
        var hackathonRequest = CreateValidHackathonRequest(Guid.NewGuid().ToString()[..8]);
        var hackathonResponse = await client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            hackathonRequest
        );
        var hackathon = await hackathonResponse.Content.ReadFromJsonAsync<HackathonResponse>();

        // Create challenge with max 1 team
        var challengeRequest = new
        {
            Title = "Limited Challenge",
            Description = "Max 1 team",
            SelectionCriteriaStmt = "currentTeamsInChallenge < 1",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon!.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Join hackathon
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);

        // Create first team
        var teamRequest1 = new { Name = "Team 1", Description = "First team" };
        var teamResponse1 = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest1
        );
        var team1 = await teamResponse1.Content.ReadFromJsonAsync<TeamResponse>();

        // First team submits - should succeed
        var submissionRequest1 = new
        {
            ChallengeId = challenge!.Id,
            Title = "First Submission",
            Summary = "First",
            RepoUri = "https://github.com/test/repo1",
            DemoUri = "https://demo1.example.com",
            SlidesUri = "https://slides1.example.com",
        };
        var submission1 = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team1!.Id}/submissions",
            submissionRequest1
        );
        await Assert.That(submission1.IsSuccessStatusCode).IsTrue();

        // Create second team
        var teamRequest2 = new { Name = "Team 2", Description = "Second team" };
        var teamResponse2 = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest2
        );
        var team2 = await teamResponse2.Content.ReadFromJsonAsync<TeamResponse>();

        // Second team tries to submit - should fail because limit is reached
        var submissionRequest2 = new
        {
            ChallengeId = challenge.Id,
            Title = "Second Submission",
            Summary = "Second",
            RepoUri = "https://github.com/test/repo2",
            DemoUri = "https://demo2.example.com",
            SlidesUri = "https://slides2.example.com",
        };
        var submission2 = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team2!.Id}/submissions",
            submissionRequest2
        );
        await Assert.That(submission2.IsSuccessStatusCode).IsFalse();
    }

    [Test]
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public async Task SubmitChallenge_WithAlwaysTrueStmt_ShouldAlwaysSucceed(
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

        // Create team
        var teamRequest = new { Name = "Test Team", Description = "Test" };
        var teamResponse = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams",
            teamRequest
        );
        var team = await teamResponse.Content.ReadFromJsonAsync<TeamResponse>();

        // Create challenge with always true statement
        var challengeRequest = new
        {
            Title = "Open Challenge",
            Description = "Open to all",
            SelectionCriteriaStmt = "true",
            IsPublished = true,
        };
        var challengeResponse = await client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/challenges",
            challengeRequest
        );
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ChallengeResponse>();

        // Submit - should succeed
        var submissionRequest = new
        {
            ChallengeId = challenge!.Id,
            Title = "My Submission",
            Summary = "Test",
            RepoUri = "https://github.com/test/repo",
            DemoUri = "https://demo.example.com",
            SlidesUri = "https://slides.example.com",
        };

        var submission = await client.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathon.Id}/teams/{team!.Id}/submissions",
            submissionRequest
        );

        await Assert.That(submission.IsSuccessStatusCode).IsTrue();
    }
}

public class TeamResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}

public class ChallengeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Criteria { get; set; } = null!;
    public bool IsPublished { get; set; }
}
