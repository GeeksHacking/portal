using GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.List;

public class Response
{
    public required IReadOnlyList<OAuthDirectoryApplicationResponse> Items { get; set; }
}
