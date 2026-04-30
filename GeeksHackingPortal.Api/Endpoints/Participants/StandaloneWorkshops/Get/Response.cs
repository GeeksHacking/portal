namespace GeeksHackingPortal.Api.Endpoints.Participants.StandaloneWorkshops.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Location { get; init; }
    public Uri? HomepageUri { get; init; }
    public required string ShortCode { get; init; }
    public required bool IsPublished { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required int MaxParticipants { get; init; }
}
