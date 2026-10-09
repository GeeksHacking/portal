using FastEndpoints;
using GeeksHackingPortal.Api.Constants;
using GeeksHackingPortal.Api.Endpoints.Auth;
using GeeksHackingPortal.Api.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;

namespace GeeksHackingPortal.Api.Endpoints.Auth.Login;

public class Endpoint(IOptions<AppOptions> options, PreviewSessionTickets tickets) : EndpointWithoutRequest
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

        var returnOrigin = LocalRedirect.OriginFromReferer(HttpContext.Request.Headers.Referer.ToString());

        // Already signed in: resume the return URL instead of challenging GitHub again.
        // Re-challenging here is what turns a failed or repeated callback into a loop.
        var existing = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        if (
            existing.Succeeded
            && existing.Principal is not null
            && !string.IsNullOrWhiteSpace(
                existing.Principal.FindFirst(CustomClaimTypes.UserId)?.Value
            )
        )
        {
            DeleteRedirectCookie();
            var destination = LocalRedirect.AppendHandoff(
                LocalRedirect.Destination(redirectUri, options.Value.FrontendUrl, returnOrigin),
                tickets.CreateHandoff(existing.Principal, returnOrigin)
            );
            await Send.RedirectAsync(destination, allowRemoteRedirects: !destination.StartsWith('/'));
            return;
        }

        if (returnOrigin is not null)
            HttpContext.Response.Cookies.Append(
                LocalRedirect.ReturnOriginCookie,
                returnOrigin,
                RedirectCookieOptions()
            );
        else
            HttpContext.Response.Cookies.Delete(LocalRedirect.ReturnOriginCookie, RedirectCookieOptions());

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
            SameSite = HttpContext.Request.Host.Host.Contains("localhost") ? SameSiteMode.Lax : SameSiteMode.None,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
        };
}
