namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Update;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TimelineItemId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}
