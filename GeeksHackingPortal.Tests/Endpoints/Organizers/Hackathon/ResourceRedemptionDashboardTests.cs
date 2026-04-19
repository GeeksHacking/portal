using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Organizers.Hackathon;

public class ResourceRedemptionDashboardTests
{
    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass client { get; init; }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient httpClient)
    {
        var whoami = await httpClient.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");
        return whoami!.Id;
    }

    [Test]
    public async Task GetResourceOverview_WithRedemptions_ReturnsParticipantStatusAndAuditTrail()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        var resource = await TestDataHelper.CreateResourceAsync(client.HttpClient, hackathon.Id);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );
        await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );

        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/resources/{resource.Id}/overview"
        );
        var result = await response.Content.ReadFromJsonAsync<OrganizerResourceOverviewResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ResourceId).IsEqualTo(resource.Id);
        await Assert.That(result.TotalRedemptions).IsEqualTo(2);
        await Assert.That(result.UniqueRedeemers).IsEqualTo(1);
        await Assert.That(result.Participants).IsNotNull();
        await Assert.That(result.Participants!.Count()).IsGreaterThan(0);

        var participant = result.Participants!.Single(p => p.UserId == participantUserId);
        await Assert.That(participant.HasRedeemed).IsTrue();
        await Assert.That(participant.RedemptionCount).IsEqualTo(2);
        await Assert.That(result.AuditTrail).IsNotNull();
        await Assert.That(result.AuditTrail!.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task GetResourceHistory_WithRedemptions_ReturnsParticipantHistory()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(client.HttpClient);
        await client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null);
        var resource = await TestDataHelper.CreateResourceAsync(client.HttpClient, hackathon.Id);
        var participantUserId = await GetCurrentUserIdAsync(client.HttpClient);

        await client.HttpClient.PostAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/redemptions",
            null
        );

        var response = await client.HttpClient.GetAsync(
            $"/organizers/hackathons/{hackathon.Id}/participants/{participantUserId}/resources/{resource.Id}/history"
        );
        var result = await response.Content.ReadFromJsonAsync<OrganizerResourceHistoryResponse>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UserId).IsEqualTo(participantUserId);
        await Assert.That(result.ResourceId).IsEqualTo(resource.Id);
        await Assert.That(result.HasRedeemed).IsTrue();
        await Assert.That(result.RedemptionCount).IsEqualTo(1);
        await Assert.That(result.History).IsNotNull();
        await Assert.That(result.History!.Count()).IsEqualTo(1);
    }
}

