namespace GeeksHackingPortal.Api.Endpoints.Organizers.Activities.StandaloneWorkshops;

public class Response
{
    public required Guid Id { get; init; }
    public Uri? HomepageUri { get; init; }
    public required string ShortCode { get; init; }
    public required int MaxParticipants { get; init; }
}
