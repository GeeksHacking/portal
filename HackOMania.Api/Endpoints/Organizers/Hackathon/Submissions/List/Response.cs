namespace HackOMania.Api.Endpoints.Organizers.Hackathon.Submissions.List;

public class Response
{
    public required List<SubmissionItem> Submissions { get; init; }
}

public class SubmissionItem
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }
    public Guid? ChallengeId { get; init; }
    public string? ChallengeTitle { get; init; }
}
