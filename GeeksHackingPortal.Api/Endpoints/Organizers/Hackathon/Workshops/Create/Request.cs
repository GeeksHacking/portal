using FastEndpoints;

namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Workshops.Create;

public class Request
{
    [BindFrom("HackathonId")]
    public Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required string Location { get; set; }
    public required int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
}
