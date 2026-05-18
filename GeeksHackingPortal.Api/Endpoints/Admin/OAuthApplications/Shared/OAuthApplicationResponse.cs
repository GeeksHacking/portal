namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

public class OAuthApplicationResponse
{
    public required string Id { get; set; }
    public required string ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public required string DisplayName { get; set; }
    public required OAuthApplicationPlatform Platform { get; set; }
    public required IReadOnlyList<Uri> RedirectUris { get; set; }
    public required IReadOnlyList<Uri> PostLogoutRedirectUris { get; set; }
}
