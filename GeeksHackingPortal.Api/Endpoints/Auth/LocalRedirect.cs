namespace GeeksHackingPortal.Api.Endpoints.Auth;

public static class LocalRedirect
{
    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('/') || value.StartsWith("//") || value.StartsWith("/\\\\"))
            return false;

        // Browsers drop tabs and newlines from URLs, so "/\t/evil.com" would become "//evil.com".
        if (value.Contains('\\') || value.Any(char.IsControl))
            return false;

        var queryIndex = value.IndexOf('?');
        var path = queryIndex >= 0 ? value[..queryIndex] : value;
        // A colon in the path looks like a scheme (open redirect). Query strings may
        // contain absolute OAuth callback URIs, so only the path is checked.
        return !path.Contains(':');
    }

    public static bool IsApiPath(string path)
    {
        var queryIndex = path.IndexOf('?');
        var pathOnly = queryIndex >= 0 ? path[..queryIndex] : path;
        return pathOnly.Equals("/connect", StringComparison.OrdinalIgnoreCase)
            || pathOnly.StartsWith("/connect/", StringComparison.OrdinalIgnoreCase)
            || pathOnly.EndsWith("/connect/authorize", StringComparison.OrdinalIgnoreCase)
            || pathOnly.EndsWith("/connect/logout", StringComparison.OrdinalIgnoreCase);
    }

    public const string ReturnOriginCookie = "auth_return_origin";

    // Preview deployments (https://<id>-portal.geekshacking.workers.dev) use the production API.
    public static bool IsAllowedFrontendOrigin(string? origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.IsDefaultPort
        && uri.AbsolutePath == "/"
        && (
            uri.Host.Equals("portal.geekshacking.workers.dev", StringComparison.OrdinalIgnoreCase)
            || (
                uri.Host.EndsWith("-portal.geekshacking.workers.dev", StringComparison.OrdinalIgnoreCase)
                && uri.Host.Length > "-portal.geekshacking.workers.dev".Length
            )
        );

    public static string? OriginFromReferer(string? referer) =>
        Uri.TryCreate(referer, UriKind.Absolute, out var uri)
        && IsAllowedFrontendOrigin(uri.GetLeftPart(UriPartial.Authority))
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;

    public static string Destination(string? redirectPath, string frontendUrl, string? returnOrigin = null)
    {
        if (IsAllowedFrontendOrigin(returnOrigin))
            frontendUrl = returnOrigin!;

        if (!IsAllowed(redirectPath))
            redirectPath = "/dash";

        // OIDC authorize/logout must resume on the API. Sending those paths to the
        // frontend makes the client retry login, which challenges GitHub again.
        if (IsApiPath(redirectPath))
            return redirectPath;

        // Mark the frontend return so a failed session check stops instead of
        // sending the browser back through /auth/login and the GitHub callback.
        var separator = redirectPath.Contains('?') ? '&' : '?';
        return $"{frontendUrl.TrimEnd('/')}{redirectPath}{separator}login_return=1";
    }

    public static string AppendHandoff(string destination, string? handoff)
    {
        if (string.IsNullOrEmpty(handoff) || destination.StartsWith('/'))
            return destination;

        var separator = destination.Contains('?') ? '&' : '?';
        return $"{destination}{separator}{PreviewSessionTickets.HandoffQuery}={Uri.EscapeDataString(handoff)}";
    }
}
