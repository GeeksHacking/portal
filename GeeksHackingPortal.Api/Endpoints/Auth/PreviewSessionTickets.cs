using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace GeeksHackingPortal.Api.Endpoints.Auth;

/// <summary>
/// Preview origins (<c>*.geekshacking.workers.dev</c>) are cross-site to the API.
/// Browsers that block third-party cookies (Brave by default, Chrome increasingly)
/// drop the <c>.geekshacking.com</c> session cookie on credentialed fetches, so a
/// successful login redirect still looks anonymous. The login navigation mints a
/// short-lived handoff; the preview exchanges it for a bearer the API accepts.
/// </summary>
public sealed class PreviewSessionTickets(
    IDataProtectionProvider dataProtection,
    IPreviewSessionHandoffStore handoffStore,
    TimeProvider timeProvider
)
{
    public const string HandoffQuery = "login_handoff";
    private const string HandoffPurpose = "handoff";
    private const string SessionPurpose = "session";

    private readonly TicketDataFormat _format = new(
        dataProtection.CreateProtector("GeeksHackingPortal.PreviewSession.v1")
    );

    public async Task<string?> CreateHandoffAsync(
        ClaimsPrincipal principal,
        string? origin,
        CancellationToken cancellationToken = default
    )
    {
        if (!LocalRedirect.IsAllowedFrontendOrigin(origin))
            return null;

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(2);
        var token = Protect(principal, origin!, HandoffPurpose, expiresAt, nonce);
        await handoffStore.RegisterAsync(HashNonce(nonce), origin!, expiresAt, cancellationToken);
        return token;
    }

    public async Task<string?> RedeemAsync(
        string? code,
        string? origin,
        CancellationToken cancellationToken = default
    )
    {
        var ticket = Unprotect(code, origin, HandoffPurpose);
        if (ticket is null)
            return null;

        if (
            !ticket.Properties.Items.TryGetValue("nonce", out var nonce)
            || string.IsNullOrWhiteSpace(nonce)
            || !await handoffStore.TryConsumeAsync(
                HashNonce(nonce),
                origin!,
                timeProvider.GetUtcNow(),
                cancellationToken
            )
        )
            return null;

        return Protect(
            ticket.Principal,
            origin!,
            SessionPurpose,
            timeProvider.GetUtcNow().AddDays(7)
        );
    }

    public ClaimsPrincipal? ReadSession(string? token, string? origin)
    {
        return Unprotect(token, origin, SessionPurpose)?.Principal;
    }

    private string Protect(
        ClaimsPrincipal principal,
        string origin,
        string purpose,
        DateTimeOffset expiresAt,
        string? nonce = null
    )
    {
        var identity = new ClaimsIdentity(
            principal.Claims.Where(c =>
                c.Type is not "access_token"
                && c.Type is not "refresh_token"
                && !c.Type.EndsWith("/access_token", StringComparison.Ordinal)
            ),
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal.Identity is ClaimsIdentity claimsIdentity
                ? claimsIdentity.NameClaimType
                : ClaimTypes.Name,
            principal.Identity is ClaimsIdentity claimsIdentityForRole
                ? claimsIdentityForRole.RoleClaimType
                : ClaimTypes.Role
        );

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IssuedUtc = timeProvider.GetUtcNow(),
                ExpiresUtc = expiresAt,
            },
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        ticket.Properties.Items["purpose"] = purpose;
        ticket.Properties.Items["origin"] = origin;
        if (nonce is not null)
            ticket.Properties.Items["nonce"] = nonce;
        return _format.Protect(ticket);
    }

    private static string HashNonce(string nonce) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(nonce)));

    private AuthenticationTicket? Unprotect(string? token, string? origin, string purpose)
    {
        if (string.IsNullOrWhiteSpace(token) || !LocalRedirect.IsAllowedFrontendOrigin(origin))
            return null;

        AuthenticationTicket? ticket;
        try
        {
            ticket = _format.Unprotect(token);
        }
        catch (Exception)
        {
            return null;
        }

        if (ticket is null)
            return null;
        if (ticket.Properties.ExpiresUtc is { } expires && expires <= timeProvider.GetUtcNow())
            return null;
        if (!ticket.Properties.Items.TryGetValue("purpose", out var ticketPurpose) || ticketPurpose != purpose)
            return null;
        if (!ticket.Properties.Items.TryGetValue("origin", out var ticketOrigin)
            || !string.Equals(ticketOrigin, origin, StringComparison.OrdinalIgnoreCase))
            return null;
        if (ticket.Principal.Identity?.IsAuthenticated != true)
            return null;

        return ticket;
    }
}
