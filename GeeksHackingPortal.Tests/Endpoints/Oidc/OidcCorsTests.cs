using GeeksHackingPortal.Tests.Data;

namespace GeeksHackingPortal.Tests.Endpoints.Oidc;

public class OidcCorsTests
{
    private const string PlaygroundOrigin = "https://oidc-playground.akamai.com";

    [ClassDataSource<HttpClientDataClass>]
    public required HttpClientDataClass Client { get; init; }

    [Test]
    public async Task DiscoveryDocument_AllowsAnyOrigin()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/.well-known/openid-configuration"
        );
        request.Headers.Add("Origin", PlaygroundOrigin);

        var response = await Client.HttpClient.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(GetCorsHeader(response, "Access-Control-Allow-Origin")).IsEqualTo("*");
    }

    [Arguments("/connect/token", "POST")]
    [Arguments("/connect/userinfo", "GET")]
    [Arguments("/connect/userinfo", "POST")]
    [Arguments("/connect/revoke", "POST")]
    [Arguments("/connect/introspect", "POST")]
    [Test]
    public async Task OpenIdConnectBrowserEndpoints_Preflight_AllowsAnyOrigin(
        string path,
        string requestedMethod
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", PlaygroundOrigin);
        request.Headers.Add("Access-Control-Request-Method", requestedMethod);
        request.Headers.Add("Access-Control-Request-Headers", "authorization, content-type");

        var response = await Client.HttpClient.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(GetCorsHeader(response, "Access-Control-Allow-Origin")).IsEqualTo("*");
        await Assert
            .That(GetCorsHeader(response, "Access-Control-Allow-Methods"))
            .IsEqualTo("GET, POST, OPTIONS");
        await Assert
            .That(GetCorsHeader(response, "Access-Control-Allow-Headers"))
            .IsEqualTo("Accept, Authorization, Content-Type");
    }

    private static string? GetCorsHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? values.SingleOrDefault() : null;
}
