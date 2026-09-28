using System.Buffers.Text;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Oidc;

// Attacks on the OAuth application redirect flow: tampering with redirect_uri at the
// authorization endpoint, misusing authorization codes at the token endpoint, tampering with
// post_logout_redirect_uri, and pointing the portal's login return URL off-site.
// No test registers attacker.example, so a redirect there is always a failure.
public class OAuthRedirectFlowSecurityTests
{
    private const string State = "state-value";
    private const string AttackerHost = "attacker.example";
    private const string RedirectUri = "https://app.example.test/callback";
    private const string OtherRedirectUri = "https://app.example.test/other-callback";
    private const string PostLogoutRedirectUri = "https://app.example.test/signed-out";
    private const string NativeRedirectUri = "com.example.app:/oauth2redirect";

    [ClassDataSource<AuthenticatedNoRedirectHttpClientDataClass>]
    public required AuthenticatedNoRedirectHttpClientDataClass AuthenticatedClient { get; init; }

    [ClassDataSource<NoRedirectHttpClientDataClass>]
    public required NoRedirectHttpClientDataClass AnonymousClient { get; init; }

    private readonly List<string> _createdApplicationIds = [];

    [After(Test)]
    public async Task DeleteCreatedApplications()
    {
        foreach (var id in _createdApplicationIds)
        {
            await AuthenticatedClient.HttpClient.DeleteAsync($"/admin/oauth-applications/{id}");
        }
    }

    [Test]
    public async Task Authorize_WithEachRegisteredRedirectUri_RedirectsThereWithCode()
    {
        string[] redirectUris = [RedirectUri, OtherRedirectUri, "http://localhost:3000/callback"];
        var application = await CreateApplicationAsync("Web", redirectUris);

        foreach (var redirectUri in redirectUris)
        {
            var response = await AuthenticatedClient.HttpClient.GetAsync(
                AuthorizeUrl(application.ClientId, redirectUri, CreatePkce().Challenge)
            );

            await AssertRedirectedWithCodeAsync(response, redirectUri);
        }
    }

    [Test]
    [Arguments("https://attacker.example/callback")]
    [Arguments("https://app.example.test.attacker.example/callback")]
    [Arguments("https://app.example.test@attacker.example/callback")]
    [Arguments("https://attacker.example/https://app.example.test/callback")]
    [Arguments("//attacker.example/callback")]
    [Arguments("/callback")]
    [Arguments("http://app.example.test/callback")]
    [Arguments("https://app.example.test:8443/callback")]
    [Arguments("https://APP.EXAMPLE.TEST/callback")]
    [Arguments("https://app.example.test/CALLBACK")]
    [Arguments("https://app.example.test/callback/")]
    [Arguments("https://app.example.test/callback/extra")]
    [Arguments("https://app.example.test/callbackextra")]
    [Arguments("https://app.example.test/")]
    [Arguments("https://app.example.test/callback/../../attacker")]
    [Arguments("https://app.example.test/callback/%2e%2e/%2e%2e/attacker")]
    [Arguments("https://app.example.test/callback?next=https://attacker.example/")]
    [Arguments("https://app.example.test/callback#@attacker.example")]
    [Arguments("https://app.example.test/callback https://attacker.example/callback")]
    [Arguments("https://app.example.test/callback,https://attacker.example/callback")]
    [Arguments("https://app.example.test/callback\r\nLocation: https://attacker.example/")]
    [Arguments("https://app.example.test/callback%0d%0aLocation:%20https://attacker.example/")]
    [Arguments("javascript:alert(document.domain)//https://app.example.test/callback")]
    [Arguments("data:text/html,<script>alert(document.domain)</script>")]
    public async Task Authorize_WithManipulatedRedirectUri_RefusesWithoutRedirecting(string redirectUri)
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, redirectUri, CreatePkce().Challenge)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    [Arguments(RedirectUri, "https://attacker.example/callback")]
    [Arguments("https://attacker.example/callback", RedirectUri)]
    public async Task Authorize_WithRepeatedRedirectUriParameter_NeverRedirectsToAttacker(
        string firstRedirectUri,
        string secondRedirectUri
    )
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, firstRedirectUri, CreatePkce().Challenge)
                + $"&redirect_uri={Uri.EscapeDataString(secondRedirectUri)}"
        );

        var target = BrowserRedirectTarget(response);
        await Assert.That(target is not null && IsAttackerHost(target)).IsFalse();
    }

    [Test]
    public async Task Authorize_WithRedirectUriRegisteredToAnotherApplication_RefusesWithoutRedirecting()
    {
        // Someone who controls another application asks for this application's code to be
        // delivered to their own registered redirect URI.
        var victim = await CreateApplicationAsync("Web", [UniqueRedirectUri()]);
        var otherApplicationRedirectUri = UniqueRedirectUri();
        await CreateApplicationAsync("Web", [otherApplicationRedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(victim.ClientId, otherApplicationRedirectUri, CreatePkce().Challenge)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Authorize_WithRedirectUriRemovedFromApplication_RefusesWithoutRedirecting()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);
        var updateResponse = await AuthenticatedClient.HttpClient.PutAsJsonAsync(
            $"/admin/oauth-applications/{application.Id}",
            new
            {
                application.ClientId,
                application.DisplayName,
                Platform = "Web",
                RedirectUris = new[] { OtherRedirectUri },
                PostLogoutRedirectUris = Array.Empty<string>(),
            }
        );

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var removedResponse = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, RedirectUri, CreatePkce().Challenge)
        );
        await AssertRefusedWithoutRedirectAsync(removedResponse);

        var keptResponse = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, OtherRedirectUri, CreatePkce().Challenge)
        );
        await AssertRedirectedWithCodeAsync(keptResponse, OtherRedirectUri);
    }

    [Test]
    public async Task Authorize_WithoutRedirectUri_RefusesWithoutRedirecting()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, redirectUri: null, CreatePkce().Challenge)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Authorize_WithUnknownClientId_RefusesWithoutRedirecting()
    {
        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(
                $"unknown-{Guid.NewGuid():N}",
                "https://attacker.example/callback",
                CreatePkce().Challenge
            )
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Authorize_WithoutSignIn_SendsBrowserToLoginWithoutIssuingCode()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);

        var response = await AnonymousClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, RedirectUri, CreatePkce().Challenge)
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var target = BrowserRedirectTarget(response);
        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Authority).IsEqualTo(response.RequestMessage!.RequestUri!.Authority);
        await Assert.That(target.AbsolutePath).IsEqualTo("/auth/login");
        await Assert.That(HttpUtility.ParseQueryString(target.Query)["redirect_uri"])
            .StartsWith("/connect/authorize?");
    }

    [Test]
    public async Task Authorize_WithoutSignIn_WithManipulatedRedirectUri_RefusesBeforeLogin()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);

        var response = await AnonymousClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, "https://attacker.example/callback", CreatePkce().Challenge)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    [Arguments("token")]
    [Arguments("id_token")]
    [Arguments("id_token token")]
    [Arguments("code id_token")]
    [Arguments("code token")]
    public async Task Authorize_WithImplicitOrHybridResponseType_DoesNotIssueCredentials(string responseType)
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, RedirectUri, CreatePkce().Challenge, responseType)
        );

        await AssertNoCredentialsIssuedAsync(response, RedirectUri);
    }

    [Test]
    public async Task Authorize_NativeApplicationWithoutPkce_DoesNotIssueCode()
    {
        var application = await CreateApplicationAsync("Native", [NativeRedirectUri]);

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, NativeRedirectUri, codeChallenge: null)
        );

        await AssertNoCredentialsIssuedAsync(response, NativeRedirectUri);
    }

    [Test]
    public async Task Token_WithRedirectUriUsedForAuthorization_IssuesTokens()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);
        var (code, pkce) = await AuthorizeAsync(application, OtherRedirectUri);

        var response = await RedeemCodeAsync(
            application.ClientId,
            application.ClientSecret,
            code,
            OtherRedirectUri,
            pkce.Verifier
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(body.TryGetProperty("access_token", out _)).IsTrue();
    }

    [Test]
    public async Task Token_WithDifferentRegisteredRedirectUri_RejectsCode()
    {
        // Both URIs belong to the application, but a code only redeems with the URI it was issued for.
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);
        var (code, pkce) = await AuthorizeAsync(application, RedirectUri);

        var response = await RedeemCodeAsync(
            application.ClientId,
            application.ClientSecret,
            code,
            OtherRedirectUri,
            pkce.Verifier
        );

        await AssertTokenRequestRejectedAsync(response, "invalid_grant");
    }

    [Test]
    public async Task Token_WithoutRedirectUri_RejectsCode()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri, OtherRedirectUri]);
        var (code, pkce) = await AuthorizeAsync(application, RedirectUri);

        var response = await RedeemCodeAsync(
            application.ClientId,
            application.ClientSecret,
            code,
            redirectUri: null,
            pkce.Verifier
        );

        await AssertTokenRequestRejectedAsync(response, "invalid_request");
    }

    [Test]
    public async Task Token_WithCodeIssuedToAnotherApplication_RejectsCode()
    {
        // A code intercepted from one application is redeemed with another application's credentials.
        var victimRedirectUri = UniqueRedirectUri();
        var victim = await CreateApplicationAsync("Web", [victimRedirectUri]);
        var other = await CreateApplicationAsync("Web", [UniqueRedirectUri()]);
        var (code, pkce) = await AuthorizeAsync(victim, victimRedirectUri);

        var response = await RedeemCodeAsync(
            other.ClientId,
            other.ClientSecret,
            code,
            victimRedirectUri,
            pkce.Verifier
        );

        await AssertTokenRequestRejectedAsync(response, "invalid_grant");
    }

    [Test]
    public async Task Token_WithReplayedCode_RejectsCodeAndRevokesIssuedTokens()
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);
        var (code, pkce) = await AuthorizeAsync(application, RedirectUri);

        var firstResponse = await RedeemCodeAsync(
            application.ClientId,
            application.ClientSecret,
            code,
            RedirectUri,
            pkce.Verifier
        );
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var accessToken = (await firstResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("access_token")
            .GetString()!;
        var userInfoBeforeReplay = await GetUserInfoAsync(accessToken);
        await Assert.That(userInfoBeforeReplay.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var replayResponse = await RedeemCodeAsync(
            application.ClientId,
            application.ClientSecret,
            code,
            RedirectUri,
            pkce.Verifier
        );

        await AssertTokenRequestRejectedAsync(replayResponse, "invalid_grant");
        var userInfoAfterReplay = await GetUserInfoAsync(accessToken);
        await Assert.That(userInfoAfterReplay.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Token_NativeApplicationWithoutMatchingCodeVerifier_RejectsCode(bool sendOtherCodeVerifier)
    {
        // An app that intercepts a native app's code cannot redeem it without the original verifier.
        var application = await CreateApplicationAsync("Native", [NativeRedirectUri]);
        var (code, _) = await AuthorizeAsync(application, NativeRedirectUri);

        var response = await RedeemCodeAsync(
            application.ClientId,
            clientSecret: null,
            code,
            NativeRedirectUri,
            sendOtherCodeVerifier ? CreatePkce().Verifier : null
        );

        await AssertTokenRequestRejectedAsync(
            response,
            sendOtherCodeVerifier ? "invalid_grant" : "invalid_request"
        );
    }

    [Test]
    [Arguments("wrong-client-secret")]
    [Arguments(null)]
    public async Task Token_WebApplicationWithoutValidClientSecret_RejectsCode(string? clientSecret)
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri]);
        var (code, pkce) = await AuthorizeAsync(application, RedirectUri);

        var response = await RedeemCodeAsync(
            application.ClientId,
            clientSecret,
            code,
            RedirectUri,
            pkce.Verifier
        );

        await AssertTokenRequestRejectedAsync(response, "invalid_client");
    }

    [Test]
    public async Task Logout_WithRegisteredPostLogoutRedirectUri_RedirectsThere()
    {
        var postLogoutRedirectUri = UniquePostLogoutRedirectUri();
        var application = await CreateApplicationAsync("Web", [RedirectUri], [postLogoutRedirectUri]);

        var response = await AnonymousClient.HttpClient.GetAsync(
            LogoutUrl(application.ClientId, postLogoutRedirectUri)
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var target = BrowserRedirectTarget(response);
        await Assert.That(target).IsNotNull();
        await Assert.That(target!.GetLeftPart(UriPartial.Path)).IsEqualTo(postLogoutRedirectUri);
    }

    [Test]
    [Arguments("https://attacker.example/signed-out")]
    [Arguments("https://app.example.test.attacker.example/signed-out")]
    [Arguments("https://app.example.test@attacker.example/signed-out")]
    [Arguments("//attacker.example/signed-out")]
    [Arguments("https://app.example.test/signed-out/../../attacker")]
    [Arguments("https://app.example.test/signed-out?next=https://attacker.example/")]
    public async Task Logout_WithManipulatedPostLogoutRedirectUri_RefusesWithoutRedirecting(
        string postLogoutRedirectUri
    )
    {
        var application = await CreateApplicationAsync("Web", [RedirectUri], [PostLogoutRedirectUri]);

        var response = await AnonymousClient.HttpClient.GetAsync(
            LogoutUrl(application.ClientId, postLogoutRedirectUri)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Logout_WithoutClientId_WithUnregisteredPostLogoutRedirectUri_RefusesWithoutRedirecting()
    {
        var response = await AnonymousClient.HttpClient.GetAsync(
            LogoutUrl(clientId: null, "https://attacker.example/signed-out")
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Logout_WithPostLogoutRedirectUriOfAnotherApplication_RefusesWithoutRedirecting()
    {
        var victim = await CreateApplicationAsync(
            "Web",
            [UniqueRedirectUri()],
            [UniquePostLogoutRedirectUri()]
        );
        var otherApplicationPostLogoutRedirectUri = UniquePostLogoutRedirectUri();
        await CreateApplicationAsync("Web", [UniqueRedirectUri()], [otherApplicationPostLogoutRedirectUri]);

        var response = await AnonymousClient.HttpClient.GetAsync(
            LogoutUrl(victim.ClientId, otherApplicationPostLogoutRedirectUri)
        );

        await AssertRefusedWithoutRedirectAsync(response);
    }

    [Test]
    public async Task Login_WhenSignedIn_WithAuthorizeReturnUrl_ResumesAuthorizationOnApi()
    {
        const string returnUrl = "/connect/authorize?client_id=portal-test&response_type=code";

        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/auth/login?redirect_uri={Uri.EscapeDataString(returnUrl)}"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var target = BrowserRedirectTarget(response);
        await Assert.That(target).IsNotNull();
        await Assert.That(target!.Authority).IsEqualTo(response.RequestMessage!.RequestUri!.Authority);
        await Assert.That(target.PathAndQuery).IsEqualTo(returnUrl);
    }

    [Test]
    [Arguments("https://attacker.example/")]
    [Arguments("//attacker.example/")]
    [Arguments("/\\attacker.example/")]
    [Arguments("/\\/attacker.example/connect/authorize")]
    [Arguments("/\t/attacker.example/")]
    [Arguments("/\t/attacker.example/connect/authorize")]
    [Arguments("/connect/authorize/../..//attacker.example/")]
    [Arguments("https:attacker.example")]
    [Arguments("/https://attacker.example/")]
    public async Task Login_WhenSignedIn_WithOffSiteReturnUrl_StaysOnPortal(string returnUrl)
    {
        var response = await AuthenticatedClient.HttpClient.GetAsync(
            $"/auth/login?redirect_uri={Uri.EscapeDataString(returnUrl)}"
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var target = BrowserRedirectTarget(response);
        await Assert.That(target).IsNotNull();
        // The API and the frontend both run on localhost in tests.
        await Assert.That(target!.Host).IsEqualTo("localhost");
    }

    private async Task<OAuthApplication> CreateApplicationAsync(
        string platform,
        string[] redirectUris,
        string[]? postLogoutRedirectUris = null
    )
    {
        var response = await AuthenticatedClient.HttpClient.PostAsJsonAsync(
            "/admin/oauth-applications",
            new
            {
                ClientId = $"test-redirect-{Guid.NewGuid():N}",
                DisplayName = "Redirect flow security test application",
                Platform = platform,
                RedirectUris = redirectUris,
                PostLogoutRedirectUris = postLogoutRedirectUris ?? [],
            }
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var application = (await response.Content.ReadFromJsonAsync<OAuthApplication>())!;
        _createdApplicationIds.Add(application.Id);
        return application;
    }

    private async Task<(string Code, Pkce Pkce)> AuthorizeAsync(
        OAuthApplication application,
        string redirectUri
    )
    {
        var pkce = CreatePkce();
        var response = await AuthenticatedClient.HttpClient.GetAsync(
            AuthorizeUrl(application.ClientId, redirectUri, pkce.Challenge)
        );

        return (await AssertRedirectedWithCodeAsync(response, redirectUri), pkce);
    }

    private Task<HttpResponseMessage> RedeemCodeAsync(
        string clientId,
        string? clientSecret,
        string code,
        string? redirectUri,
        string? codeVerifier
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["code"] = code,
        };
        if (clientSecret is not null)
            form["client_secret"] = clientSecret;
        if (redirectUri is not null)
            form["redirect_uri"] = redirectUri;
        if (codeVerifier is not null)
            form["code_verifier"] = codeVerifier;

        return AnonymousClient.HttpClient.PostAsync("/connect/token", new FormUrlEncodedContent(form));
    }

    private Task<HttpResponseMessage> GetUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return AnonymousClient.HttpClient.SendAsync(request);
    }

    private static string AuthorizeUrl(
        string clientId,
        string? redirectUri,
        string? codeChallenge,
        string responseType = "code"
    ) =>
        WithQuery(
            "/connect/authorize",
            ("client_id", clientId),
            ("redirect_uri", redirectUri),
            ("response_type", responseType),
            ("scope", "openid profile"),
            ("state", State),
            ("nonce", "nonce-value"),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", codeChallenge is null ? null : "S256")
        );

    private static string LogoutUrl(string? clientId, string postLogoutRedirectUri) =>
        WithQuery(
            "/connect/logout",
            ("client_id", clientId),
            ("post_logout_redirect_uri", postLogoutRedirectUri),
            ("state", State)
        );

    private static string WithQuery(string path, params (string Name, string? Value)[] parameters) =>
        path
        + "?"
        + string.Join(
            "&",
            parameters
                .Where(parameter => parameter.Value is not null)
                .Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value!)}")
        );

    private static Pkce CreatePkce()
    {
        var verifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return new Pkce(verifier, challenge);
    }

    private static string UniqueRedirectUri() => $"https://{Guid.NewGuid():N}.app.example.test/callback";

    private static string UniquePostLogoutRedirectUri() =>
        $"https://{Guid.NewGuid():N}.app.example.test/signed-out";

    // Where a browser would go for this response's Location header, or null without one.
    // Browsers drop tabs and newlines from URLs and treat '\' as '/', so resolve it the same way.
    private static Uri? BrowserRedirectTarget(HttpResponseMessage response)
    {
        if (!response.Headers.NonValidated.TryGetValues("Location", out var values))
            return null;

        var location = new string(
                values.ToString().Where(c => c is not ('\t' or '\r' or '\n')).ToArray()
            )
            .Replace('\\', '/');
        return new Uri(response.RequestMessage!.RequestUri!, location);
    }

    private static bool IsAttackerHost(Uri uri) =>
        uri.Host == AttackerHost || uri.Host.EndsWith($".{AttackerHost}", StringComparison.Ordinal);

    private static NameValueCollection ResponseParameters(Uri uri)
    {
        var parameters = HttpUtility.ParseQueryString(uri.Query);
        parameters.Add(HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#')));
        return parameters;
    }

    private static async Task AssertRefusedWithoutRedirectAsync(HttpResponseMessage response)
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Headers.NonValidated.Contains("Location")).IsFalse();
    }

    private static async Task<string> AssertRedirectedWithCodeAsync(
        HttpResponseMessage response,
        string redirectUri
    )
    {
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        var target = BrowserRedirectTarget(response);
        await Assert.That(target).IsNotNull();
        await Assert.That(target!.GetLeftPart(UriPartial.Path)).IsEqualTo(redirectUri);

        var parameters = ResponseParameters(target);
        await Assert.That(parameters["state"]).IsEqualTo(State);
        var code = parameters["code"];
        await Assert.That(string.IsNullOrEmpty(code)).IsFalse();
        return code!;
    }

    // Errors may be returned directly or reported to the registered redirect URI, never with
    // a code or tokens.
    private static async Task AssertNoCredentialsIssuedAsync(
        HttpResponseMessage response,
        string redirectUri
    )
    {
        var target = BrowserRedirectTarget(response);
        if (target is null)
        {
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            return;
        }

        await Assert.That(target.GetLeftPart(UriPartial.Path)).IsEqualTo(redirectUri);
        var parameters = ResponseParameters(target);
        await Assert.That(parameters["error"]).IsNotNull();
        await Assert.That(parameters["code"]).IsNull();
        await Assert.That(parameters["access_token"]).IsNull();
        await Assert.That(parameters["id_token"]).IsNull();
    }

    private static async Task AssertTokenRequestRejectedAsync(
        HttpResponseMessage response,
        string expectedError
    )
    {
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(body.GetProperty("error").GetString()).IsEqualTo(expectedError);
        await Assert.That(body.TryGetProperty("access_token", out _)).IsFalse();
    }

    private sealed record Pkce(string Verifier, string Challenge);

    private sealed class OAuthApplication
    {
        public required string Id { get; init; }
        public required string ClientId { get; init; }
        public string? ClientSecret { get; init; }
        public required string DisplayName { get; init; }
    }
}
