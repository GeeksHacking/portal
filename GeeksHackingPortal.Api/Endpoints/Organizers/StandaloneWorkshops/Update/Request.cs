namespace GeeksHackingPortal.Api.Endpoints.Organizers.StandaloneWorkshops.Update;

public class Request
{
    public Guid StandaloneWorkshopId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public string? Location { get; set; }

    public Uri? HomepageUri { get; set; }

    public string? ShortCode { get; set; }

    public int? MaxParticipants { get; set; }

    public bool? IsPublished { get; set; }

    public Dictionary<string, string>? EmailTemplates { get; set; }
}
