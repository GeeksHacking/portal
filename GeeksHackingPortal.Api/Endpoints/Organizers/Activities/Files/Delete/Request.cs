using FastEndpoints;
using GeeksHackingPortal.Api.Features.ActivityFiles;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.Delete;

public class Request : ActivityFileRouteRequest
{
    [RouteParam]
    public Guid FileId { get; set; }
}
