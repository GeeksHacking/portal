namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.StandaloneWorkshops;

public class Request
{
    public Guid ActivityId { get; set; }
    public Uri? HomepageUri { get; set; }
    public string? ShortCode { get; set; }
    public int? MaxParticipants { get; set; }
}
