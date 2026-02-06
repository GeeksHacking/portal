namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Timeline.Update;

public class Response
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
