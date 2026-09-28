using FastEndpoints;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Endpoints.Auth;
using GeeksHackingPortal.Api.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace GeeksHackingPortal.Api.Endpoints.Auth.Login;

public class Endpoint(IOptions<AppOptions> options) : EndpointWithoutRequest
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
        if (!LocalRedirect.IsAllowed(redirectUri))
            redirectUri = null;

        // Already signed in: resume the return URL instead of challenging GitHub again.
        // Re-challenging here is what turns a failed or repeated callback into a loop.
        var existing = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        if (
            existing.Succeeded
            && !string.IsNullOrWhiteSpace(
                existing.Principal?.FindFirst(CustomClaimTypes.UserId)?.Value
            )
        )
        {
            DeleteRedirectCookie();
            var destination = LocalRedirect.Destination(redirectUri, options.Value.FrontendUrl);
            await Send.RedirectAsync(destination, allowRemoteRedirects: !destination.StartsWith('/'));
            return;
        }

        var properties = new AuthenticationProperties { RedirectUri = redirectUri ?? "/" };

        if (redirectUri is not null)
        {
            properties.Items["redirect_uri"] = redirectUri;

            // Also store in cookie as backup since OpenIddict state may not preserve Items
            HttpContext.Response.Cookies.Append(
                "auth_redirect_uri",
                redirectUri,
                RedirectCookieOptions()
            );
        }

        await Send.ResultAsync(
            Results.Challenge(properties: properties, authenticationSchemes: [Providers.GitHub])
        );
    }

    private void DeleteRedirectCookie()
    {
        var cookieOptions = RedirectCookieOptions();
        cookieOptions.MaxAge = null;
        HttpContext.Response.Cookies.Delete("auth_redirect_uri", cookieOptions);
    }

    private CookieOptions RedirectCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = !HttpContext.Request.Host.Host.Contains("localhost"),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
        };
}
