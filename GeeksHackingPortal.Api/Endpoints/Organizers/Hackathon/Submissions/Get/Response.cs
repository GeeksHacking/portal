namespace GeeksHackingPortal.Api.Endpoints.Organizers.Hackathon.Submissions.Get;

public class Response
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Uri RepositoryUri { get; init; }
    public required Uri DemoUri { get; init; }
    public required Uri SlidesUri { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }
    public required Guid ChallengeId { get; init; }
    public required string ChallengeTitle { get; init; }
}
