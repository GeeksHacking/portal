using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthDirectory.Shared;

public class OAuthDirectoryApplicationResponse
{
    public required string Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public required OAuthApplicationPlatform Platform { get; set; }
    public string? ClientType { get; set; }
    public string? ConsentType { get; set; }
    public required IReadOnlyList<Uri> RedirectUris { get; set; }
    public required IReadOnlyList<Uri> PostLogoutRedirectUris { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public required int TotalAuthorizations { get; set; }
    public required int ValidAuthorizations { get; set; }
    public required int UniqueUsers { get; set; }
    public required int TotalTokens { get; set; }
    public DateTimeOffset? LastAuthorizedAt { get; set; }
    public DateTimeOffset? LastTokenIssuedAt { get; set; }
}
