using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using GeeksHackingPortal.Api.Endpoints.Auth;
using GeeksHackingPortal.Tests.Data;
using Microsoft.AspNetCore.DataProtection;

namespace GeeksHackingPortal.Tests.Endpoints.Auth;

public class PreviewSessionHandoffTests
{
    private const string PreviewOrigin = "https://test-portal.geekshacking.workers.dev";

    [ClassDataSource<AuthenticatedNoRedirectHttpClientDataClass>]
    public required AuthenticatedNoRedirectHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task Handoff_RedeemsOnceAndAuthenticatesPreviewRequests()
    {
        var code = await IssueHandoffAsync();
        var redemptions = await Task.WhenAll(
            RedeemAsync(code, PreviewOrigin),
            RedeemAsync(code, PreviewOrigin)
        );
        await Assert.That(redemptions.Count(response => response.StatusCode == HttpStatusCode.OK))
            .IsEqualTo(1);
        await Assert.That(
            redemptions.Count(response => response.StatusCode == HttpStatusCode.Unauthorized)
        ).IsEqualTo(1);
        var response = redemptions.Single(response => response.StatusCode == HttpStatusCode.OK);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<HandoffResponse>();
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.Token).IsNotEmpty();

        using var whoAmI = new HttpRequestMessage(HttpMethod.Get, "/auth/whoami");
        whoAmI.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        whoAmI.Headers.Add("Origin", PreviewOrigin);
        var whoAmIResponse = await Client.HttpClient.SendAsync(whoAmI);
        await Assert.That(whoAmIResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var replay = await RedeemAsync(code, PreviewOrigin);
        await Assert.That(replay.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Handoff_RequiresMatchingOriginWithoutConsumingCode()
    {
        var code = await IssueHandoffAsync();

        var missingOrigin = await RedeemAsync(code, null);
        await Assert.That(missingOrigin.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        var mismatchedOrigin = await RedeemAsync(code, "https://other-portal.geekshacking.workers.dev");
        await Assert.That(mismatchedOrigin.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        var correctOrigin = await RedeemAsync(code, PreviewOrigin);
        await Assert.That(correctOrigin.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Handoff_RejectsSessionTokenAsHandoffCode()
    {
        var code = await IssueHandoffAsync();
        var redeemed = await RedeemAsync(code, PreviewOrigin);
        await Assert.That(redeemed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var token = await redeemed.Content.ReadFromJsonAsync<HandoffResponse>();
        await Assert.That(token).IsNotNull();

        var wrongPurpose = await RedeemAsync(token!.Token, PreviewOrigin);
        await Assert.That(wrongPurpose.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Handoff_ExpiredTicketCannotBeRedeemed()
    {
        using var services = new ServiceCollection()
            .AddDataProtection()
            .UseEphemeralDataProtectionProvider()
            .Services.BuildServiceProvider();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var tickets = new PreviewSessionTickets(
            services.GetRequiredService<IDataProtectionProvider>(),
            clock
        );
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "test")
        );

        var code = await tickets.CreateHandoffAsync(principal, PreviewOrigin);
        clock.Advance(TimeSpan.FromMinutes(2));
        var redeemed = await tickets.RedeemAsync(code, PreviewOrigin);

        await Assert.That(redeemed).IsNull();
    }

    [Test]
    public async Task AppendHandoff_PlacesQueryBeforeFragment()
    {
        var destination = LocalRedirect.AppendHandoff(
            $"{PreviewOrigin}/dash?tab=profile#settings",
            "one-time-code"
        );

        await Assert.That(destination)
            .IsEqualTo($"{PreviewOrigin}/dash?tab=profile&login_handoff=one-time-code#settings");
    }

    private async Task<string> IssueHandoffAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/login?redirect_uri=%2Fdash");
        request.Headers.Referrer = new Uri($"{PreviewOrigin}/");
        using var response = await AuthenticatedClient.HttpClient.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var location = response.Headers.Location;
        await Assert.That(location).IsNotNull();
        var query = location!.Query.TrimStart('?')
            .Split('&')
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(parts =>
                Uri.UnescapeDataString(parts[0]) == PreviewSessionTickets.HandoffQuery
            );
        await Assert.That(query).IsNotNull();
        return Uri.UnescapeDataString(query![1]);
    }

    private async Task<HttpResponseMessage> RedeemAsync(string code, string? origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/handoff")
        {
            Content = JsonContent.Create(new { code }),
        };
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        return await Client.HttpClient.SendAsync(request);
    }

    private sealed class HandoffResponse
    {
        public string Token { get; init; } = "";
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now += amount;
    }
}
