namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Timeline.Create;

public class Request
{
    public Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}
