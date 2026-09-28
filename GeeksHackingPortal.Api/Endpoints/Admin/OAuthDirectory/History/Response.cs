namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.History;

public class Response
{
    public required int TotalCount { get; set; }
    public required IReadOnlyList<OAuthDirectoryHistoryItemResponse> Items { get; set; }
}

public class OAuthDirectoryHistoryItemResponse
{
    public required string Id { get; set; }
    public required string Subject { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? CreationDate { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public required IReadOnlyList<string> Scopes { get; set; }
    public required int TokenCount { get; set; }
    public DateTime? LastTokenIssuedAt { get; set; }
}
