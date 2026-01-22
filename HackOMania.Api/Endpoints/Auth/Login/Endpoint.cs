using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace HackOMania.Api.Endpoints.Auth.Login;

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
        if (!string.IsNullOrEmpty(redirectUri) &&
            redirectUri.StartsWith('/') &&
            !redirectUri.Contains("://"))
        {
            properties.Items["redirect_uri"] = redirectUri;
        }

        await Send.ResultAsync(
            Results.Challenge(
                properties: properties,
                authenticationSchemes: [Providers.GitHub]
            )
        );
    }
}
