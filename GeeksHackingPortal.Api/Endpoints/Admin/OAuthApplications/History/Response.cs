namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.History;

public class Response
{
    public List<OAuthApplicationHistoryItemResponse> Items { get; set; } = [];
}

public class OAuthApplicationHistoryItemResponse
{
    public string Id { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public DateTime? CreationDate { get; set; }
    public string? Scopes { get; set; }
}
