using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace GeeksHackingPortal.Api.Endpoints.Auth.Login;

public class Endpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("auth/login");
        AllowAnonymous();
        Description(d => d.ExcludeFromDescription());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var redirectUri = Query<string>("redirect_uri", isRequired: false);

        var properties = new AuthenticationProperties { RedirectUri = "/" };

        // Validate redirect_uri to prevent open redirect attacks
        // Must start with "/" and not contain "://" (prevents //evil.com or http://evil.com)
        if (
            !string.IsNullOrEmpty(redirectUri)
            && redirectUri.StartsWith('/')
            && !redirectUri.Contains("://")
        )
        {
            properties.Items["redirect_uri"] = redirectUri;

            // Also store in cookie as backup since OpenIddict state may not preserve Items
            HttpContext.Response.Cookies.Append(
                "auth_redirect_uri",
                redirectUri,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !HttpContext.Request.Host.Host.Contains("localhost"),
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(10),
                    Path = "/",
                }
            );
        }

        await Send.ResultAsync(
            Results.Challenge(properties: properties, authenticationSchemes: [Providers.GitHub])
        );
    }
}
