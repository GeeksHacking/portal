using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Analytics;

public class Response
{
    public List<OAuthApplicationAnalyticsResponse> Items { get; set; } = [];
}

public class OAuthApplicationAnalyticsResponse
{
    public string ApplicationId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public OAuthApplicationPlatform Platform { get; set; }
    public int TotalAuthorizations { get; set; }
    public int UniqueUsers { get; set; }
}
