using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.List;

public class Response
{
    public required IReadOnlyList<OAuthApplicationResponse> Items { get; set; }
}
