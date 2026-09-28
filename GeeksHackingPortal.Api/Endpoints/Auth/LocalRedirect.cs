namespace GeeksHackingPortal.Api.Endpoints.Auth;

public static class LocalRedirect
{
    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('/') || value.StartsWith("//") || value.StartsWith("/\\"))
            return false;

        if (value.Contains('\\') || value.IndexOfAny(['\r', '\n', '\0']) >= 0)
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

    public static string Destination(string? redirectPath, string frontendUrl)
    {
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
}
