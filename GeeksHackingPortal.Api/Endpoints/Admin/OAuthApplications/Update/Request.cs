using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Update;

public class Request : OAuthApplicationMutationRequest
{
    public string Id { get; set; } = string.Empty;
    public bool RotateClientSecret { get; set; }
}
