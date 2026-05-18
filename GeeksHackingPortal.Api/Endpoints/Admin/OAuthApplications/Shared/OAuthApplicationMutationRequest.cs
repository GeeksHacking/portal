namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

public class OAuthApplicationMutationRequest
{
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public required OAuthApplicationPlatform Platform { get; set; }
    public required List<Uri> RedirectUris { get; set; }
    public List<Uri> PostLogoutRedirectUris { get; set; } = [];
}
