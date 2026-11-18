using GeeksHackingPortal.Api.Features.ActivityFiles;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Create;

public class Request : ActivityFileRouteRequest
{
    public IFormFile File { get; set; } = null!;
}
