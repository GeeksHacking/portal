using GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Get;

public class Response
{
    public required OAuthDirectoryApplicationResponse Application { get; set; }
    public required IReadOnlyList<string> Permissions { get; set; }
    public required IReadOnlyList<string> Requirements { get; set; }
}
