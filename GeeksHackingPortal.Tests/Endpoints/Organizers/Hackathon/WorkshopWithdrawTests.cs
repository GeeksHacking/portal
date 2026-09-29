using System.Net;
using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Endpoints.Participants.Hackathon;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class WorkshopWithdrawTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

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
            GitHubLogin = $"ws-participant-{suffix}",
            FirstName = "Participant",
            LastName = suffix,
            Email = $"ws-participant-{suffix}@example.com",
        };

        await client.InitializeAsync();
        return client;
    }

    private async Task<Guid> CreateHackathonAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString()[..8];
        var response = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/hackathons",
            new CreateHackathonRequest
            {
                Name = $"Workshop Withdraw Test {suffix}",
                Description = "A test hackathon for workshop withdraw tests",
                Venue = "Virtual",
                HomepageUri = new Uri("https://example.com/hackathon"),
                ShortCode = $"WWD{suffix}",
                EventStartDate = now.AddDays(7),
                EventEndDate = now.AddDays(9),
                SubmissionsStartDate = now.AddDays(7).AddHours(2),
                SubmissionsEndDate = now.AddDays(8).AddHours(20),
                JudgingStartDate = now.AddDays(8).AddHours(21),
                JudgingEndDate = now.AddDays(9).AddHours(-2),
                IsPublished = true,
            }
        );
        var hackathon = await response.Content.ReadFromJsonAsync<HackathonResponse>();
        return hackathon!.Id;
    }

    private async Task<Guid> CreateWorkshopAsync(Guid hackathonId, int maxParticipants = 50)
    {
        var response = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathonId}/workshops",
            new
            {
                Title = "Withdraw Test Workshop",
                Description = "A workshop to withdraw from",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                Location = "Room 101",
                MaxParticipants = maxParticipants,
                IsPublished = true,
            }
        );
        var workshop = await response.Content.ReadFromJsonAsync<WorkshopResponse>();
        return workshop!.Id;
    }

    private static async Task<Guid> GetUserIdAsync(AuthenticatedHttpClientDataClass client)
    {
        var whoAmI = await client.HttpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
        return whoAmI!.Id;
    }

    private static async Task JoinHackathonAndWorkshopAsync(
        AuthenticatedHttpClientDataClass participant,
        Guid hackathonId,
        Guid workshopId
    )
    {
        var joinHackathon = await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        await Assert.That(joinHackathon.IsSuccessStatusCode).IsTrue();

        var joinWorkshop = await participant.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/join",
            new { }
        );
        await Assert.That(joinWorkshop.IsSuccessStatusCode).IsTrue();
    }

    private static async Task<bool> IsJoinedAsync(
        AuthenticatedHttpClientDataClass participant,
        Guid hackathonId,
        Guid workshopId
    )
    {
        var list = await participant.HttpClient.GetFromJsonAsync<ParticipantWorkshopListResponse>(
            $"/participants/hackathons/{hackathonId}/workshops"
        );
        return list!.Workshops.Single(w => w.Id == workshopId).IsJoined;
    }

    [Test]
    public async Task ParticipantWithdraw_AfterJoining_ReturnsOkAndLeavesWorkshop()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await JoinHackathonAndWorkshopAsync(participant, hackathonId, workshopId);

        var response = await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsJoinedAsync(participant, hackathonId, workshopId)).IsFalse();
    }

    [Test]
    public async Task ParticipantWithdraw_ThenRejoin_Succeeds()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await JoinHackathonAndWorkshopAsync(participant, hackathonId, workshopId);

        await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/withdraw",
            null
        );
        var rejoin = await participant.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/join",
            new { }
        );

        await Assert.That(rejoin.IsSuccessStatusCode).IsTrue();
        await Assert.That(await IsJoinedAsync(participant, hackathonId, workshopId)).IsTrue();
    }

    [Test]
    public async Task ParticipantWithdraw_FreesCapacity()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId, maxParticipants: 1);
        await using var first = await CreateParticipantClientAsync();
        await using var second = await CreateParticipantClientAsync();
        await JoinHackathonAndWorkshopAsync(first, hackathonId, workshopId);
        await second.HttpClient.PostAsync($"/participants/hackathons/{hackathonId}/join", null);

        var full = await second.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/join",
            new { }
        );
        await Assert.That(full.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        await first.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/withdraw",
            null
        );
        var joined = await second.HttpClient.PostAsJsonAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/join",
            new { }
        );

        await Assert.That(joined.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task ParticipantWithdraw_WhenNotJoinedToWorkshop_ReturnsNotFound()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );

        var response = await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{workshopId}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ParticipantWithdraw_WithUnknownWorkshop_ReturnsNotFound()
    {
        var hackathonId = await CreateHackathonAsync();
        await using var participant = await CreateParticipantClientAsync();
        await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );

        var response = await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/workshops/{Guid.NewGuid()}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task OrganizerWithdraw_RemovesParticipantFromWorkshop()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await JoinHackathonAndWorkshopAsync(participant, hackathonId, workshopId);
        var participantUserId = await GetUserIdAsync(participant);

        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/workshops/{workshopId}/participants/{participantUserId}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await IsJoinedAsync(participant, hackathonId, workshopId)).IsFalse();
    }

    [Test]
    public async Task OrganizerWithdraw_WhenParticipantNotInWorkshop_ReturnsNotFound()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await participant.HttpClient.PostAsync(
            $"/participants/hackathons/{hackathonId}/join",
            null
        );
        var participantUserId = await GetUserIdAsync(participant);

        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/workshops/{workshopId}/participants/{participantUserId}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task OrganizerWithdraw_WithUnknownWorkshop_ReturnsNotFound()
    {
        var hackathonId = await CreateHackathonAsync();

        var response = await Client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/workshops/{Guid.NewGuid()}/participants/{Guid.NewGuid()}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task OrganizerWithdraw_AsNonOrganizer_IsForbidden()
    {
        var hackathonId = await CreateHackathonAsync();
        var workshopId = await CreateWorkshopAsync(hackathonId);
        await using var participant = await CreateParticipantClientAsync();
        await JoinHackathonAndWorkshopAsync(participant, hackathonId, workshopId);
        var participantUserId = await GetUserIdAsync(participant);

        var response = await participant.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathonId}/workshops/{workshopId}/participants/{participantUserId}/withdraw",
            null
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        await Assert.That(await IsJoinedAsync(participant, hackathonId, workshopId)).IsTrue();
    }
}
