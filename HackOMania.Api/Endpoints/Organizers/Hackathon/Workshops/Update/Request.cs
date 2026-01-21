using FastEndpoints;

namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Workshops.Update;

public class Request
{
    [BindFrom("HackathonId")]
    public Guid HackathonId { get; set; }

    [BindFrom("WorkshopId")]
    public Guid WorkshopId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required string Location { get; set; }
    public required int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
}
