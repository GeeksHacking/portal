using GeeksHackingPortal.Api.Features.ActivityFiles;

namespace GeeksHackingPortal.Api.Endpoints.Participants.Activities.Files.List;

public class Response
{
    public required List<ActivityFileResponse> Files { get; init; }
}
