namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Delete;

public class Request
{
    public Guid HackathonId { get; set; }
    public Guid TimelineItemId { get; set; }
}
