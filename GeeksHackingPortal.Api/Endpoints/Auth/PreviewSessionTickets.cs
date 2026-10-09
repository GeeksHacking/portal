using System.Security.Claims;
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
public sealed class PreviewSessionTickets(IDataProtectionProvider dataProtection)
{
    public const string HandoffQuery = "login_handoff";
    private const string HandoffPurpose = "handoff";
    private const string SessionPurpose = "session";

    private readonly TicketDataFormat _format = new(
        dataProtection.CreateProtector("GeeksHackingPortal.PreviewSession.v1")
    );

    public string? CreateHandoff(ClaimsPrincipal principal, string? origin)
    {
        if (!LocalRedirect.IsAllowedFrontendOrigin(origin))
            return null;

        return Protect(principal, origin!, HandoffPurpose, TimeSpan.FromMinutes(2));
    }

    public string? Redeem(string? code, string? origin)
    {
        var ticket = Unprotect(code, origin, HandoffPurpose);
        if (ticket is null)
            return null;

        return Protect(ticket.Principal, origin!, SessionPurpose, TimeSpan.FromDays(7));
    }

    public ClaimsPrincipal? ReadSession(string? token, string? origin)
    {
        return Unprotect(token, origin, SessionPurpose)?.Principal;
    }

    private string Protect(ClaimsPrincipal principal, string origin, string purpose, TimeSpan lifetime)
    {
        var identity = new ClaimsIdentity(
            principal.Claims.Where(c =>
                c.Type is not "access_token"
                && c.Type is not "refresh_token"
                && !c.Type.EndsWith("/access_token", StringComparison.Ordinal)
            ),
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal.Identity?.NameClaimType ?? ClaimTypes.Name,
            principal.Identity?.RoleClaimType ?? ClaimTypes.Role
        );

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(lifetime),
            },
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        ticket.Properties.Items["purpose"] = purpose;
        ticket.Properties.Items["origin"] = origin;
        return _format.Protect(ticket);
    }

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
        if (ticket.Properties.ExpiresUtc is { } expires && expires < DateTimeOffset.UtcNow)
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
