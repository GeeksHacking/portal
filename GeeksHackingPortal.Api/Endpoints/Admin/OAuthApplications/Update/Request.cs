using GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Shared;

namespace GeeksHackingPortal.Api.Endpoints.Admin.OAuthApplications.Update;

public class Request : OAuthApplicationMutationRequest
{
    public required string Id { get; set; }
    public bool RotateClientSecret { get; set; }
}
