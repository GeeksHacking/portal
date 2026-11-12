using GeeksHackingPortal.Api.Features.ActivityFiles;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.Files.List;

public class Response
{
    public ActivityFileResponse? Banner { get; init; }

    public required List<ActivityFileResponse> Files { get; init; }
}
