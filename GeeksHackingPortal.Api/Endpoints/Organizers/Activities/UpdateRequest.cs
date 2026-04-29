namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities;

public class UpdateRequest
{
    public Guid ActivityId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public string? Location { get; set; }
    public bool? IsPublished { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
}
