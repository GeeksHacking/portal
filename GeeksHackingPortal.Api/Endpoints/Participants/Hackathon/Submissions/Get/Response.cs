namespace GeeksHackingPortal.Api.Endpoints.Participants.Hackathon.Submissions.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public Uri? DevpostUri { get; init; }
    public Uri? RepositoryUri { get; init; }
    public Uri? DemoUri { get; init; }
    public Uri? SlidesUri { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }
    public Guid? ChallengeId { get; init; }
    public string? ChallengeTitle { get; init; }
}
