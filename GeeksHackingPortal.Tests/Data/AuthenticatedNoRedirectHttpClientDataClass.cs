namespace GeeksHackingPortal.Tests.Data;

// Signed-in client that returns redirects instead of following them, so tests can
// inspect where the API would send the browser.
public class AuthenticatedNoRedirectHttpClientDataClass : AuthenticatedHttpClientDataClass
{
    protected override bool AllowAutoRedirect => false;
}
