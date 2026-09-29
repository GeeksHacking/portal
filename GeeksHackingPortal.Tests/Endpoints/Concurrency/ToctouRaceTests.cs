using System.Net.Http.Json;
using GeeksHackingPortal.Tests.Data;
using GeeksHackingPortal.Tests.Helpers;
using GeeksHackingPortal.Tests.Models;

namespace GeeksHackingPortal.Tests.Endpoints.Concurrency;

/// <summary>
/// Time-of-check to time-of-use regression tests. Each test fires many identical requests in
/// parallel at an endpoint that performs a check (capacity, quota, uniqueness) followed by a
/// write, and asserts the invariant the check is meant to protect still holds.
/// </summary>
public class ToctouRaceTests
{
    private const int Parallelism = 8;

    [ClassDataSource<AuthenticatedHttpClientDataClass>]
    public required AuthenticatedHttpClientDataClass Client { get; init; }

    private static async Task<AuthenticatedHttpClientDataClass> CreateUserClientAsync()
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
            GitHubLogin = $"toctou-{suffix}",
            FirstName = "Toctou",
            LastName = suffix,
            Email = $"toctou-{suffix}@example.com",
        };

        await client.InitializeAsync();
        return client;
    }

    private static async Task<AuthenticatedHttpClientDataClass[]> CreateUserClientsAsync(int count) =>
        await Task.WhenAll(Enumerable.Range(0, count).Select(_ => CreateUserClientAsync()));

    private static async Task DisposeAllAsync(IEnumerable<AuthenticatedHttpClientDataClass> clients)
    {
        foreach (var client in clients)
        {
            await client.DisposeAsync();
        }
    }

    private static async Task<HttpResponseMessage[]> FireInParallelAsync(
        int count,
        Func<int, Task<HttpResponseMessage>> request
    )
    {
        // Release all requests at the same instant to maximise the chance of interleaving.
        var gate = new TaskCompletionSource();
        var tasks = Enumerable
            .Range(0, count)
            .Select(async i =>
            {
                await gate.Task;
                return await request(i);
            })
            .ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    [Test]
    public async Task AcceptInvite_ConcurrentRedemptions_NeverExceedMaxUses()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);
        var inviteResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/hackathons/{hackathon.Id}/organizers/invites",
            new { Type = "Volunteer", MaxUses = 1 }
        );
        var invite = await inviteResponse.Content.ReadFromJsonAsync<OrganizerInviteResponse>();
        var users = await CreateUserClientsAsync(Parallelism);

        try
        {
            var responses = await FireInParallelAsync(
                users.Length,
                i =>
                    users[i]
                        .HttpClient.PostAsJsonAsync(
                            "/organizers/accept-invite",
                            new { Code = invite!.Code }
                        )
            );

            var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            await Assert.That(succeeded).IsEqualTo(1);

            var organizersResponse = await Client.HttpClient.GetAsync(
                $"/organizers/hackathons/{hackathon.Id}/organizers"
            );
            var organizers = await organizersResponse.Content.ReadFromJsonAsync<OrganizersListResponse>();
            // The creator plus exactly one redeemer.
            await Assert.That(organizers!.Organizers!.Count()).IsEqualTo(2);
        }
        finally
        {
            await DisposeAllAsync(users);
        }
    }

    [Test]
    public async Task JoinStandaloneWorkshop_ConcurrentJoins_NeverExceedCapacity()
    {
        var request = TestDataHelper.CreateValidStandaloneWorkshopRequest();
        request.MaxParticipants = 2;
        var createResponse = await Client.HttpClient.PostAsJsonAsync(
            "/organizers/standalone-workshops",
            request
        );
        var workshop = await createResponse.Content.ReadFromJsonAsync<StandaloneWorkshopResponse>();
        var users = await CreateUserClientsAsync(Parallelism);

        try
        {
            var responses = await FireInParallelAsync(
                users.Length,
                i =>
                    users[i]
                        .HttpClient.PostAsync(
                            $"/participants/standalone-workshops/{workshop!.Id}/join",
                            null
                        )
            );

            await Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.OK)).IsEqualTo(2);

            var listResponse = await Client.HttpClient.GetAsync(
                $"/organizers/standalone-workshops/{workshop!.Id}/participants"
            );
            var participants =
                await listResponse.Content.ReadFromJsonAsync<StandaloneParticipantListResponse>();
            await Assert.That(participants!.Participants.Count).IsEqualTo(2);
        }
        finally
        {
            await DisposeAllAsync(users);
        }
    }

    [Test]
    public async Task RedeemResource_ConcurrentRedemptions_RespectRedemptionLimit()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );
        var listResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/participants"
        );
        var participants =
            await listResponse.Content.ReadFromJsonAsync<StandaloneParticipantListResponse>();
        var participant = participants!.Participants.Single();

        var resourceResponse = await Client.HttpClient.PostAsJsonAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/resources",
            new
            {
                Name = "One Per Person",
                Description = "Can be redeemed once per participant",
                RedemptionStmt = "return participantRedemptions === 0;",
                IsPublished = true,
            }
        );
        var resource = await resourceResponse.Content.ReadFromJsonAsync<ResourceResponse>();

        var responses = await FireInParallelAsync(
            Parallelism,
            _ =>
                Client.HttpClient.PostAsJsonAsync(
                    $"/organizers/standalone-workshops/{workshop.Id}/participants/{participant.UserId}/resources/{resource!.Id}/redemptions",
                    new { }
                )
        );

        await Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.OK)).IsEqualTo(1);
        await Assert
            .That(responses.Count(r => r.StatusCode == HttpStatusCode.Forbidden))
            .IsEqualTo(Parallelism - 1);
    }

    [Test]
    public async Task CheckIn_ConcurrentScans_CreateSingleActiveCheckIn()
    {
        var workshop = await TestDataHelper.CreateStandaloneWorkshopAsync(Client.HttpClient);
        await Client.HttpClient.PostAsync(
            $"/participants/standalone-workshops/{workshop.Id}/join",
            null
        );
        var listResponse = await Client.HttpClient.GetAsync(
            $"/organizers/standalone-workshops/{workshop.Id}/participants"
        );
        var participants =
            await listResponse.Content.ReadFromJsonAsync<StandaloneParticipantListResponse>();
        var participant = participants!.Participants.Single();

        var responses = await FireInParallelAsync(
            Parallelism,
            _ =>
                Client.HttpClient.PostAsJsonAsync(
                    $"/organizers/standalone-workshops/{workshop.Id}/participants/{participant.UserId}/venue/check-in",
                    new { }
                )
        );

        await Assert.That(responses.All(r => r.StatusCode == HttpStatusCode.OK)).IsTrue();

        // Every request must observe the same single active check-in.
        var ids = new HashSet<Guid>();
        foreach (var response in responses)
        {
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            ids.Add(body.GetProperty("id").GetGuid());
        }

        await Assert.That(ids.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CreateTeam_ConcurrentRequestsFromSameParticipant_CreateSingleTeam()
    {
        var hackathon = await TestDataHelper.CreateHackathonAndJoinAsync(Client.HttpClient);

        var responses = await FireInParallelAsync(
            Parallelism,
            i =>
                Client.HttpClient.PostAsJsonAsync(
                    $"/participants/hackathons/{hackathon.Id}/teams",
                    new { Name = $"Race Team {i}", Description = "Concurrent create" }
                )
        );

        await Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.OK)).IsEqualTo(1);
        await Assert.That(responses.Any(r => (int)r.StatusCode >= 500)).IsFalse();
    }

    [Test]
    public async Task JoinHackathon_ConcurrentJoinsFromSameUser_AreIdempotent()
    {
        var hackathon = await TestDataHelper.CreateHackathonAsync(Client.HttpClient);

        var responses = await FireInParallelAsync(
            Parallelism,
            _ => Client.HttpClient.PostAsync($"/participants/hackathons/{hackathon.Id}/join", null)
        );

        await Assert.That(responses.All(r => r.StatusCode == HttpStatusCode.OK)).IsTrue();
    }

    private sealed class StandaloneParticipantListResponse
    {
        public List<StandaloneParticipantResponse> Participants { get; set; } = [];
    }

    private sealed class StandaloneParticipantResponse
    {
        public Guid UserId { get; set; }
    }
}
