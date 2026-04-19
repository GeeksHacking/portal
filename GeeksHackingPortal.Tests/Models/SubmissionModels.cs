namespace GeeksHackingPortal.Tests.Models;

public class SubmissionsListResponse
{
    public IEnumerable<SubmissionItem>? Submissions { get; set; }
}

public class SubmissionItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid TeamId { get; set; }
    public string? TeamName { get; set; }
    public Guid ChallengeId { get; set; }
    public string? ChallengeTitle { get; set; }
}

public class TeamSubmissionItem
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public Uri? RepoUri { get; set; }
    public Uri? DemoUri { get; set; }
    public Uri? SlidesUri { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}

public class TeamSubmissionsListResponse
{
    public IEnumerable<TeamSubmissionItem>? Submissions { get; set; }
}

public class CreateSubmissionRequest
{
    public required Guid ChallengeId { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public Uri? RepoUri { get; set; }
    public Uri? DemoUri { get; set; }
    public Uri? SlidesUri { get; set; }
}

public class CreateSubmissionResponse
{
    public Guid Id { get; set; }
    public Guid HackathonId { get; set; }
    public Guid TeamId { get; set; }
    public Guid ChallengeId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Uri? RepoUri { get; set; }
    public Uri? DemoUri { get; set; }
    public Uri? SlidesUri { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
