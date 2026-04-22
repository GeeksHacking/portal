namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Create;

public class Request
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required string Location { get; set; }
    public required Uri HomepageUri { get; set; }
    public required string ShortCode { get; set; }
    public required int MaxParticipants { get; set; }
    public bool IsPublished { get; set; }
    public Dictionary<string, string>? EmailTemplates { get; set; }
}
